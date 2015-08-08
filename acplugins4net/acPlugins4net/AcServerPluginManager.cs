using acPlugins4net.configuration;
using acPlugins4net.helpers;
using acPlugins4net.kunos;
using acPlugins4net.messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net
{
    public class AcServerPluginManager : ILog
    {
        #region private fields
        private readonly DuplexUDPClient _UDP;
        private readonly WorkaroundHelper _Workarounds;
        private readonly List<AcServerPluginBase> _plugins;
        private readonly List<ExternalPluginInfo> _externalPlugins;
        private readonly Dictionary<ExternalPluginInfo, DuplexUDPClient> _openExternalPlugins;
        #endregion

        #region public fields/properties
        public readonly ILog Logger;
        public readonly IConfigManager Config;

        public readonly ReadOnlyCollection<AcServerPluginBase> Plugins;
        public readonly ReadOnlyCollection<ExternalPluginInfo> ExternalPlugins;

        /// <summary>
        /// Gets or sets whether requests to the AC server should be logged.
        /// Can be set via app.config setting "log_server_requests". Default is 1.
        /// Currently implemented: 0 = Off and 1 = On
        /// </summary>
        public int LogServerRequests { get; set; }

        /// <summary>
        /// Gets or sets the port on which the plugin manager receives messages from the AC server.
        /// Can be set via app.config setting "plugin_port". Default is 12000.
        /// </summary>
        public int ListeningPort { get; set; }

        /// <summary>
        /// Gets or sets the hostname of the AC server.
        /// Can be set via app.config setting "ac_server_host". Default is "127.0.0.1".
        /// </summary>
        public string RemostHostname { get; set; }

        /// <summary>
        /// Gets or sets the port of the AC server where requests should be send to.
        /// Can be set via app.config setting "ac_server_port". Default is 11000.
        /// </summary>
        public int RemotePort { get; set; }

        public string ServerName { get; set; }
        public string Track { get; set; }
        public string TrackLayout { get; set; }
        public int MaxClients { get; set; }
        #endregion

        public AcServerPluginManager(ILog log = null, IConfigManager config = null)
        {
            Logger = log ?? new ConsoleLogger();
            Config = config ?? new AppConfigConfigurator();

            _plugins = new List<AcServerPluginBase>();
            Plugins = _plugins.AsReadOnly();

            _UDP = new DuplexUDPClient();
            _Workarounds = new WorkaroundHelper(Config);

            _externalPlugins = new List<ExternalPluginInfo>();
            ExternalPlugins = _externalPlugins.AsReadOnly();

            _openExternalPlugins = new Dictionary<ExternalPluginInfo, DuplexUDPClient>();

            // get the configured ports (app.config)
            ListeningPort = Config.GetSettingAsInt("plugin_port", 12000);
            RemostHostname = Config.GetSetting("ac_server_host");
            if (string.IsNullOrWhiteSpace(RemostHostname))
                RemostHostname = "127.0.0.1";
            RemotePort = Config.GetSettingAsInt("ac_server_port", 11000);
            LogServerRequests = Config.GetSettingAsInt("log_server_requests", 1);
        }

        /// <summary>
        /// Loads the information from server configuration.
        /// The following Properties are set: <see cref="ServerName"/>, <see cref="Track"/>, <see cref="TrackLayout"/>, 
        /// <see cref="MaxClients"/>, <see cref="ListeningPort"/>,  <see cref="RemostHostname"/>, <see cref="RemotePort"/>
        /// </summary>
        public void LoadInfoFromServerConfig()
        {
            ServerName = _Workarounds.FindServerConfigEntry("NAME=");
            Track = _Workarounds.FindServerConfigEntry("TRACK=");
            TrackLayout = _Workarounds.FindServerConfigEntry("CONFIG_TRACK=");
            MaxClients = Convert.ToInt32(_Workarounds.FindServerConfigEntry("MAX_CLIENTS="));

            // First we're getting the configured ports (read directly from the server_config.ini)
            string acServerPortString = _Workarounds.FindServerConfigEntry("UDP_PLUGIN_LOCAL_PORT=");
            string pluginPortString = _Workarounds.FindServerConfigEntry("UDP_PLUGIN_ADDRESS=");

            #region determine the acServerPort with helpful error messages - this *will* be done wrong
            if (string.IsNullOrWhiteSpace(acServerPortString) || acServerPortString == "0")
                throw new Exception("There is no UDP_PLUGIN_LOCAL_PORT defined in the server_config.ini - check the file and the path in the <plugin>.exe.config");

            int acServerPort;
            if (!int.TryParse(acServerPortString, out acServerPort))
                throw new Exception("Error in server_config.ini: UDP_PLUGIN_LOCAL_PORT=" + acServerPortString + " is not a valid port - check the file and the path in the <plugin>.exe.config");
            #endregion

            #region the same for the plugin port - including a restriction to localhost (see http://www.assettocorsa.net/forum/index.php?threads/about-that-maybe-server-api.24360/page-8#post-507070)
            if (string.IsNullOrWhiteSpace(pluginPortString))
                throw new Exception("There is no UDP_PLUGIN_ADDRESS defined in the server_config.ini - check the file and the path in the <plugin>.exe.config");

            if (!pluginPortString.StartsWith("127.0.0.1:"))
                throw new Exception("The UDP_PLUGIN_ADDRESS (defined in the server_config.ini) must referenced locally, that is 127.0.0.1:<port> - check the file and the path in the <plugin>.exe.config");

            int pluginPort;
            if (!int.TryParse(pluginPortString.Replace("127.0.0.1:", ""), out pluginPort))
                throw new Exception("Error in server_config.ini: UDP_PLUGIN_ADDRESS=" + pluginPortString + " is not a valid port - check the file and the path in the <plugin>.exe.config");
            #endregion

            ListeningPort = pluginPort;
            RemostHostname = "127.0.0.1";
            RemotePort = acServerPort;
        }

        public void LoadPluginsFromAppConfig()
        {
            // try to load plugins configured in app.config
            try
            {
                string pluginsStr = Config.GetSetting("internal_plugins");
                if (!string.IsNullOrWhiteSpace(pluginsStr))
                {
                    foreach (string pluginTypeStr in pluginsStr.Split(';'))
                    {
                        try
                        {
                            string[] typeInfo = pluginTypeStr.Split(',');
                            Assembly assembly = Assembly.Load(typeInfo[1]);
                            Type type = assembly.GetType(typeInfo[0]);
                            AcServerPluginBase plugin = (AcServerPluginBase)Activator.CreateInstance(type);
                            this.AddPlugin(plugin);
                        }
                        catch (Exception ex)
                        {
                            Log(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }

            // try to load info about external plugins in app.config
            try
            {
                string externalPluginsStr = Config.GetSetting("external_plugins");
                if (!string.IsNullOrWhiteSpace(externalPluginsStr))
                {
                    foreach (string pluginInfoStr in externalPluginsStr.Split(';'))
                    {
                        try
                        {
                            string[] parts = pluginInfoStr.Split(',');
                            string[] remotePluginParts = parts[2].Split(':');

                            this.AddExternalPlugin(new ExternalPluginInfo(
                                parts[0].Trim(),
                                int.Parse(parts[1]),
                                remotePluginParts[0].Trim(),
                                int.Parse(remotePluginParts[1])));
                        }
                        catch (Exception ex)
                        {
                            Log(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }


        public void AddPlugin(AcServerPluginBase plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException("plugin");
            }

            if (IsConnected)
            {
                throw new Exception("Cannot add plugin while connected.");
            }

            if (_plugins.Contains(plugin))
            {
                throw new Exception("Plugin was added before.");
            }

            _plugins.Add(plugin);

            plugin.OnInitBase(this);
        }

        public void RemovePlugin(AcServerPluginBase plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException("plugin");
            }

            if (IsConnected)
            {
                throw new Exception("Cannot remove plugin while connected.");
            }

            if (!_plugins.Contains(plugin))
            {
                throw new Exception("Plugin was not added before.");
            }

            _plugins.Remove(plugin);
        }

        public void AddExternalPlugin(ExternalPluginInfo externalPlugin)
        {
            if (externalPlugin == null)
            {
                throw new ArgumentNullException("externalPlugin");
            }

            if (IsConnected)
            {
                throw new Exception("Cannot add external plugin while connected.");
            }

            if (_externalPlugins.Contains(externalPlugin))
            {
                throw new Exception("External plugin was added before.");
            }

            _externalPlugins.Add(externalPlugin);
        }

        public void RemoveExternalPlugin(ExternalPluginInfo externalPlugin)
        {
            if (externalPlugin == null)
            {
                throw new ArgumentNullException("externalPlugin");
            }

            if (IsConnected)
            {
                throw new Exception("Cannot remove external plugin while connected.");
            }

            if (!_externalPlugins.Contains(externalPlugin))
            {
                throw new Exception("External plugin was not added before.");
            }

            _externalPlugins.Remove(externalPlugin);
        }

        public bool IsConnected
        {
            get
            {
                return _UDP.Opened;
            }
        }

        public virtual void Connect()
        {
            if (IsConnected)
            {
                throw new Exception("PluginManager already connected");
            }

            _UDP.Open(ListeningPort, RemostHostname, RemotePort, MessageReceived, Log);

            foreach (AcServerPluginBase plugin in _plugins)
            {
                try
                {
                    plugin.OnConnectedBase();
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }

            foreach (ExternalPluginInfo externalPlugin in _externalPlugins)
            {
                try
                {
                    DuplexUDPClient externalPluginUdp = new DuplexUDPClient();
                    externalPluginUdp.Open(externalPlugin.ListeningPort, externalPlugin.RemostHostname, externalPlugin.RemotePort, MessageReceivedFromExternalPlugin, Log);
                    _openExternalPlugins.Add(externalPlugin, externalPluginUdp);
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        protected virtual void MessageReceivedFromExternalPlugin(byte[] data)
        {
            _UDP.Send(data);

            if (LogServerRequests > 0)
            {
                LogRequestToServer(AcMessageParser.Parse(data));
            }
        }

        public virtual void Disconnect()
        {
            if (!IsConnected)
            {
                throw new Exception("PluginManager is not connected");
            }

            _UDP.Close();

            foreach (DuplexUDPClient externalPluginUdp in _openExternalPlugins.Values)
            {
                try
                {
                    externalPluginUdp.Close();
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
            _openExternalPlugins.Clear();

            foreach (AcServerPluginBase plugin in _plugins)
            {
                try
                {
                    plugin.OnDisconnectedBase();
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        protected virtual void MessageReceived(byte[] data)
        {
            var msg = AcMessageParser.Parse(data);

            foreach (AcServerPluginBase plugin in _plugins)
            {
                try
                {
                    switch (msg.Type)
                    {
                        case ACSProtocol.MessageType.ACSP_NEW_SESSION:
                            plugin.OnNewSessionBase((MsgNewSession)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_NEW_CONNECTION:
                            plugin.OnNewConnectionBase((MsgNewConnection)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_CONNECTION_CLOSED:
                            plugin.OnConnectionClosedBase((MsgConnectionClosed)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_CAR_UPDATE:
                            plugin.OnCarUpdateBase((MsgCarUpdate)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_CAR_INFO:
                            plugin.OnCarInfoBase((MsgCarInfo)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_LAP_COMPLETED:
                            plugin.OnLapCompletedBase((MsgLapCompleted)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_END_SESSION:
                            plugin.OnSessionEndedBase((MsgSessionEnded)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_CLIENT_EVENT:
                            plugin.OnCollisionBase((MsgClientEvent)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_REALTIMEPOS_INTERVAL:
                        case ACSProtocol.MessageType.ACSP_GET_CAR_INFO:
                        case ACSProtocol.MessageType.ACSP_SEND_CHAT:
                        case ACSProtocol.MessageType.ACSP_BROADCAST_CHAT:
                            throw new Exception("Received unexpected MessageType (for a plugin): " + msg.Type);
                        case ACSProtocol.MessageType.ACSP_CE_COLLISION_WITH_CAR:
                        case ACSProtocol.MessageType.ACSP_CE_COLLISION_WITH_ENV:
                        case ACSProtocol.MessageType.ERROR:
                        default:
                            throw new Exception("Received wrong or unknown MessageType: " + msg.Type);
                    }
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }

            foreach (DuplexUDPClient externalPluginUdp in _openExternalPlugins.Values)
            {
                externalPluginUdp.TrySend(data);
            }
        }

        public void ProcessEnteredCommand(string cmd)
        {
            foreach (AcServerPluginBase plugin in _plugins)
            {
                try
                {
                    if (!plugin.OnCommandEnteredBase(cmd))
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        #region Requests to the AcServer

        public virtual void RequestCarInfo(byte carId)
        {
            var carInfoRequest = new RequestCarInfo() { CarId = carId };
            _UDP.Send(carInfoRequest.ToBinary());
            if (LogServerRequests > 0)
            {
                LogRequestToServer(carInfoRequest);
            }
        }

        public virtual void BroadcastChatMessage(string msg)
        {
            var chatRequest = new RequestBroadcastChat() { ChatMessage = msg };
            _UDP.Send(chatRequest.ToBinary());
            if (LogServerRequests > 0)
            {
                LogRequestToServer(chatRequest);
            }
        }

        public virtual void SendChatMessage(byte car_id, string msg)
        {
            var chatRequest = new RequestSendChat() { CarId = car_id, ChatMessage = msg };
            _UDP.Send(chatRequest.ToBinary());
            if (LogServerRequests > 0)
            {
                LogRequestToServer(chatRequest);
            }
        }

        public virtual void EnableRealtimeReport(UInt16 interval)
        {
            var enableRealtimeReportRequest = new RequestRealtimeInfo { Interval = interval };
            _UDP.Send(enableRealtimeReportRequest.ToBinary());
            if (LogServerRequests > 0)
            {
                LogRequestToServer(enableRealtimeReportRequest);
            }
        }

        #endregion

        #region for convenience ILog is implemented by plugin manager

        public virtual void Log(string message)
        {
            Logger.Log(message);
        }

        public virtual void Log(Exception ex)
        {
            Logger.Log(ex);
        }

        #endregion

        protected virtual void LogRequestToServer(PluginMessage msg)
        {
            Log("Sent Request: " + msg);
        }
    }
}
