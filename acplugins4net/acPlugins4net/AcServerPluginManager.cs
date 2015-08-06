using acPlugins4net.configuration;
using acPlugins4net.helpers;
using acPlugins4net.kunos;
using acPlugins4net.messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net
{

    public class ExternalPlugin
    {
        public readonly int ListeningPort;
        public readonly string RemostHostname;
        public readonly int RemotePort;

        public ExternalPlugin(int listeningPort, string remostHostname, int remotePort)
        {
            ListeningPort = listeningPort;
            RemostHostname = remostHostname;
            RemotePort = remotePort;
        }
    }

    public class AcServerPluginManager
    {
        public readonly IConfigManager Config;
        public readonly ILog Log;

        private readonly DuplexUDPClient _UDP;
        private readonly WorkaroundHelper _Workarounds = null;

        private readonly List<IAcServerPlugin> _plugins;
        public readonly ReadOnlyCollection<IAcServerPlugin> Plugins;

        private readonly List<ExternalPlugin> _externalPlugins;
        public readonly ReadOnlyCollection<ExternalPlugin> ExternalPlugins;

        private readonly Dictionary<ExternalPlugin, DuplexUDPClient> _openExternalPlugins;

        public string ServerName { get; set; }
        public string Track { get; set; }
        public string TrackLayout { get; set; }
        public int MaxClients { get; set; }

        public AcServerPluginManager(ILog log = null, IConfigManager config = null)
        {
            this.Log = log != null ? log : new ConsoleLogger();
            this.Config = config != null ? config : new AppConfigConfigurator();

            _plugins = new List<IAcServerPlugin>();
            Plugins = _plugins.AsReadOnly();

            _UDP = new DuplexUDPClient();
            _Workarounds = new WorkaroundHelper(this.Config);

            _externalPlugins = new List<ExternalPlugin>();
            ExternalPlugins = _externalPlugins.AsReadOnly();

            _openExternalPlugins = new Dictionary<ExternalPlugin, DuplexUDPClient>();
        }

        public void LoadInfoFromServerConfig()
        {
            try
            {
                ServerName = _Workarounds.FindServerConfigEntry("NAME=");
                Track = _Workarounds.FindServerConfigEntry("TRACK=");
                TrackLayout = _Workarounds.FindServerConfigEntry("CONFIG_TRACK=");
                MaxClients = Convert.ToInt32(_Workarounds.FindServerConfigEntry("MAX_CLIENTS="));
            }
            catch (Exception ex)
            {
                this.OnError(ex);
            }
        }

        public void AddPlugin(IAcServerPlugin plugin)
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

            plugin.OnInit(this);
        }

        public void RemovePlugin(IAcServerPlugin plugin)
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

        public void AddExternalPlugin(ExternalPlugin externalPlugin)
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

        public void RemoveExternalPlugin(ExternalPlugin externalPlugin)
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

        public virtual void OnError(Exception ex)
        {
            if (this.Log != null)
            {
                this.Log.Log(ex);
            }
        }

        public bool IsConnected
        {
            get
            {
                return this._UDP.Opened;
            }
        }

        public virtual void Connect()
        {
            // First we're getting the configured ports (app.config)
            var acServerHost = Config.GetSetting("acServer_host", "127.0.0.1");
            var acServerPort = Config.GetSettingAsInt("acServer_port", 11000);
            var pluginPort = Config.GetSettingAsInt("plugin_port", 12000);

            _UDP.Open(pluginPort, acServerHost, acServerPort, MessageReceived, OnError);

            foreach (IAcServerPlugin plugin in _plugins)
            {
                try
                {
                    plugin.OnConnected();
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }

            foreach (ExternalPlugin externalPlugin in _externalPlugins)
            {
                try
                {
                    DuplexUDPClient externalPluginUdp = new DuplexUDPClient();
                    externalPluginUdp.Open(externalPlugin.ListeningPort, externalPlugin.RemostHostname, externalPlugin.RemotePort, MessageReceivedFromExternalPlugin, OnError);
                    _openExternalPlugins.Add(externalPlugin, externalPluginUdp);
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        protected virtual void MessageReceivedFromExternalPlugin(byte[] data)
        {
            this._UDP.TrySend(data);
        }

        public virtual void Disconnect()
        {
            _UDP.Close();

            foreach (DuplexUDPClient externalPluginUdp in _openExternalPlugins.Values)
            {
                try
                {
                    externalPluginUdp.Close();
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
            _openExternalPlugins.Clear();

            foreach (IAcServerPlugin plugin in _plugins)
            {
                try
                {
                    plugin.OnDisconnected();
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        protected virtual void MessageReceived(byte[] data)
        {
            var msg = AcMessageParser.Parse(data);

            foreach (IAcServerPlugin plugin in _plugins)
            {
                try
                {
                    switch (msg.Type)
                    {
                        case ACSProtocol.MessageType.ACSP_NEW_SESSION:
                            plugin.OnNewSession((MsgNewSession)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_NEW_CONNECTION:
                            plugin.OnNewConnection((MsgNewConnection)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_CONNECTION_CLOSED:
                            plugin.OnConnectionClosed((MsgConnectionClosed)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_CAR_UPDATE:
                            plugin.OnCarUpdate((MsgCarUpdate)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_CAR_INFO:
                            plugin.OnCarInfo((MsgCarInfo)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_LAP_COMPLETED:
                            plugin.OnLapCompleted((MsgLapCompleted)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_END_SESSION:
                            plugin.OnSessionEnded((MsgSessionEnded)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_CLIENT_EVENT:
                            plugin.OnCollision((MsgClientEvent)msg);
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
                    OnError(ex);
                }
            }

            foreach (DuplexUDPClient externalPluginUdp in _openExternalPlugins.Values)
            {
                try
                {
                    externalPluginUdp.TrySend(data);
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        public void OnConsoleCommand(string cmd)
        {
            foreach (IAcServerPlugin plugin in _plugins)
            {
                try
                {
                    if (!plugin.OnConsoleCommand(cmd))
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        #region Requests to the AcServer

        public virtual void RequestCarInfo(byte carId)
        {
            var carInfoRequest = new RequestCarInfo() { CarId = carId };
            _UDP.TrySend(carInfoRequest.ToBinary());
        }

        public virtual void BroadcastChatMessage(string msg)
        {
            var chatRequest = new RequestBroadcastChat() { ChatMessage = msg };
            _UDP.TrySend(chatRequest.ToBinary());
        }

        public virtual void SendChatMessage(byte car_id, string msg)
        {
            var chatRequest = new RequestSendChat() { CarId = car_id, ChatMessage = msg };
            _UDP.TrySend(chatRequest.ToBinary());
        }

        public virtual void EnableRealtimeReport(UInt16 interval)
        {
            var enableRealtimeReportRequest = new RequestRealtimeInfo { Interval = interval };
            _UDP.TrySend(enableRealtimeReportRequest.ToBinary());
        }

        #endregion
    }
}
