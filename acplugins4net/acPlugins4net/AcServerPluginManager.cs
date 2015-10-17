using acPlugins4net.info;
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
using System.Threading;
using System.Threading.Tasks;

namespace acPlugins4net
{
    public sealed class AcServerPluginManager : ILog
    {
        static AcServerPluginManager()
        {
            RequiredProtocolVersion = Assembly.GetExecutingAssembly().GetName().Version.Major;
        }

        public static readonly int RequiredProtocolVersion;
        public bool IgnoreRequiredProtocolVersion { get; set; }

        #region private fields
        private readonly DuplexUDPClient _UDP;
        private readonly WorkaroundHelper _Workarounds;
        private readonly List<AcServerPlugin> _plugins;
        private readonly List<ExternalPluginInfo> _externalPlugins;
        private readonly Dictionary<ExternalPluginInfo, DuplexUDPClient> _openExternalPlugins;
        private readonly object lockObject = new object();
        private readonly bool PortsFromAppConfig;
        #endregion

        #region public fields/properties
        public readonly ILog Logger;
        public readonly IConfigManager Config;

        public readonly ReadOnlyCollection<AcServerPlugin> Plugins;
        public readonly ReadOnlyCollection<ExternalPluginInfo> ExternalPlugins;

        public readonly List<ISessionReportHandler> SessionReportHandlers = new List<ISessionReportHandler>();

        /// <summary>
        /// The acServer UDP Protocol Version.
        /// </summary>
        public int ProtocolVersion { get; private set; }

        public ushort RealtimeUpdateInterval { get; private set; }

        public int StartNewLogOnNewSession { get; set; }

        /// <summary>
        /// Gets or sets whether requests to the AC server should be logged.
        /// Can be set via app.config setting "log_server_requests". Default is 1.
        /// Currently implemented: 0 = Off and 1 = On
        /// </summary>
        public int LogServerRequests { get; set; }

        public int LogServerErrors { get; set; }

        /// <summary>
        /// Gets or sets the port on which the plugin manager receives messages from the AC server.
        /// Can be set via app.config setting "plugin_port". Default is 12000.
        /// </summary>
        public int ListeningPort { get; set; }

        /// <summary>
        /// Gets or sets the hostname of the AC server.
        /// Can be set via app.config setting "ac_server_host". Default is "127.0.0.1".
        /// </summary>
        public string RemoteHostname { get; set; }

        /// <summary>
        /// Gets or sets the port of the AC server where requests should be send to.
        /// Can be set via app.config setting "ac_server_port". Default is 11000.
        /// </summary>
        public int RemotePort { get; set; }

        public string AdminPassword { get; set; }

        public int ServerBlacklistMode { get; set; } = -1;
        #endregion

        #region session info stuff
        private readonly Dictionary<byte, DriverInfo> carUsedByDictionary = new Dictionary<byte, DriverInfo>();
        private int lastCarUpdateCarId = -1;
        private SessionInfo currentSession = new SessionInfo();
        private SessionInfo previousSession = new SessionInfo();

        public SessionInfo CurrentSession
        {
            get { return currentSession; }
        }

        public SessionInfo PreviousSession
        {
            get { return previousSession; }
        }

        public bool TryGetDriverInfo(byte carId, out DriverInfo driver)
        {
            lock (lockObject)
            {
                return carUsedByDictionary.TryGetValue(carId, out driver);
            }
        }

        public DriverInfo GetDriverInfo(byte carId)
        {
            DriverInfo driver;
            if (this.TryGetDriverInfo(carId, out driver))
            {
                return driver;
            }
            else
            {
                return null;
            }
        }

        public DriverInfo GetDriverByConnectionId(int connectionId)
        {
            return this.currentSession.Drivers[connectionId];
        }
        #endregion

        public AcServerPluginManager(ILog log = null, IConfigManager config = null)
        {
            Logger = log ?? new ConsoleLogger();
            Config = config ?? new AppConfigConfigurator();


            _plugins = new List<AcServerPlugin>();
            Plugins = _plugins.AsReadOnly();

            _UDP = new DuplexUDPClient();
            _Workarounds = new WorkaroundHelper(Config);

            _externalPlugins = new List<ExternalPluginInfo>();
            ExternalPlugins = _externalPlugins.AsReadOnly();

            _openExternalPlugins = new Dictionary<ExternalPluginInfo, DuplexUDPClient>();

            ProtocolVersion = -1;

            // get the configured ports (app.config)
            ListeningPort = Config.GetSettingAsInt("plugin_port", 0);
            RemotePort = Config.GetSettingAsInt("ac_server_port", 0);
            if (ListeningPort != 0 && RemotePort != 0)
            {
                PortsFromAppConfig = true;
            }
            else
            {
                ListeningPort = 12000;
                RemotePort = 11000;
            }

            // get the remote hostname
            RemoteHostname = Config.GetSetting("ac_server_host");
            if (string.IsNullOrWhiteSpace(RemoteHostname))
                RemoteHostname = "127.0.0.1";

            this.currentSession.MaxClients = Config.GetSettingAsInt("max_clients", 32); // TODO can be removed when MaxClients added to MsgSessionInfo
            AdminPassword = Config.GetSetting("admin_password");

            StartNewLogOnNewSession = Config.GetSettingAsInt("start_new_log_on_new_session", 0);
            LogServerRequests = Config.GetSettingAsInt("log_server_requests", 1);
            LogServerErrors = Config.GetSettingAsInt("log_server_errors", 1);

            this.RealtimeUpdateInterval = (ushort)this.Config.GetSettingAsInt("realtime_update_interval", 1000);
            string sessionReportHandlerType = this.Config.GetSetting("session_report_handlers");
            if (!string.IsNullOrWhiteSpace(sessionReportHandlerType))
            {
                foreach (string handlerTypeStr in sessionReportHandlerType.Split(';'))
                {
                    string[] typeInfo = handlerTypeStr.Split(',');
                    Assembly assembly = Assembly.Load(typeInfo[1]);
                    Type type = assembly.GetType(typeInfo[0]);
                    ISessionReportHandler reportHandler = (ISessionReportHandler)Activator.CreateInstance(type);
                    this.SessionReportHandlers.Add(reportHandler);
                }
            }

            // Let the admin override the protocol version - 1.3 has issues with the linux server
            IgnoreRequiredProtocolVersion = Config.GetSetting("ignore_UPD_protocol_version") == "true";
            if (IgnoreRequiredProtocolVersion)
                Log("IgnoreRequiredProtocolVersion set to 'true', will ignore the message protocol version. Please disable this in case of trouble");
        }

        /// <summary>
        /// Loads the information from server configuration.
        /// The following Properties are set: <see cref="MaxClients"/>, <see cref="ListeningPort"/>,  <see cref="RemoteHostname"/>, <see cref="RemotePort"/>
        /// </summary>
        public void LoadInfoFromServerConfig()
        {
            lock (lockObject)
            {
                if (this.Config.GetSettingAsInt("load_server_cfg", 1) == 0)
                {
                    return;
                }

                this.currentSession.MaxClients = Convert.ToInt32(_Workarounds.FindServerConfigEntry("MAX_CLIENTS=")); // TODO can be removed when MaxClients added to MsgSessionInfo
                AdminPassword = _Workarounds.FindServerConfigEntry("ADMIN_PASSWORD=");
                ServerBlacklistMode = Convert.ToInt32(_Workarounds.FindServerConfigEntry("BLACKLIST_MODE="));

                // We will parse the server's plugin config only if the stuff isn't already defined by the overriden ac_server_port / plugin_port
                // This is important to run the behind a proxy or redirector (that is directly attached to the acServer then)
                if (this.PortsFromAppConfig)
                {
                    return;
                }

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
                RemotePort = acServerPort;
            }
        }

        public void LoadPluginsFromAppConfig()
        {
            lock (lockObject)
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
                                AcServerPlugin plugin = (AcServerPlugin)Activator.CreateInstance(type);
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
        }


        public void AddPlugin(AcServerPlugin plugin)
        {
            lock (lockObject)
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

                plugin.Initialize(this);
            }
        }

        public void RemovePlugin(AcServerPlugin plugin)
        {
            lock (lockObject)
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
        }

        public void AddExternalPlugin(ExternalPluginInfo externalPlugin)
        {
            lock (lockObject)
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
        }

        public void RemoveExternalPlugin(ExternalPluginInfo externalPlugin)
        {
            lock (lockObject)
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
        }

        private DriverInfo getDriverReportForCarId(byte carId)
        {
            DriverInfo driverReport;
            if (!carUsedByDictionary.TryGetValue(carId, out driverReport))
            {
                // it seems we missed the OnNewConnection for this driver
                driverReport = new DriverInfo()
                {
                    ConnectionId = this.currentSession.Drivers.Count(),
                    ConnectedTimestamp = DateTime.UtcNow.Ticks, //obviously not correct but better than nothing
                    CarId = carId
                };

                this.currentSession.Drivers.Add(driverReport);
                this.carUsedByDictionary.Add(driverReport.CarId, driverReport);
                this.RequestCarInfo(carId);
            }
            else if (string.IsNullOrEmpty(driverReport.DriverGuid))
            {
                // it seems we did not yet receive carInfo yet, request again
                this.RequestCarInfo(carId);
            }

            return driverReport;
        }

        private void SetSessionInfo(MsgSessionInfo msg, bool startNewLog)
        {
            this.currentSession.ServerName = msg.ServerName;
            this.currentSession.TrackName = msg.Track;
            this.currentSession.TrackConfig = msg.TrackConfig;
            this.currentSession.SessionName = msg.Name;
            this.currentSession.SessionType = msg.SessionType;
            this.currentSession.SessionDuration = msg.SessionDuration;
            this.currentSession.LapCount = msg.Laps;
            this.currentSession.WaitTime = msg.WaitTime;
            this.currentSession.Timestamp = DateTime.UtcNow.Ticks;
            this.currentSession.AmbientTemp = msg.AmbientTemp;
            this.currentSession.RoadTemp = msg.RoadTemp;
            this.currentSession.Weather = msg.Weather;
            this.currentSession.RealtimeUpdateInterval = this.RealtimeUpdateInterval;
            // TODO set MaxClients when added to msg

            if (startNewLog && this.StartNewLogOnNewSession > 0 && this.Logger is IFileLog)
            {
                ((IFileLog)this.Logger).StartLoggingToFile(
                    new DateTime(this.currentSession.Timestamp, DateTimeKind.Utc).ToString("yyyyMMdd_HHmmss") + "_"
                    + this.currentSession.TrackName + "_" + this.currentSession.SessionName + ".log");
            }
        }

        private void FinalizeAndStartNewReport()
        {
            try
            {
                // update PlayerConnections with results
                foreach (DriverInfo connection in this.currentSession.Drivers)
                {
                    List<LapInfo> laps = this.currentSession.Laps.Where(l => l.ConnectionId == connection.ConnectionId).ToList();
                    List<LapInfo> validLaps = laps.Where(l => l.Cuts == 0).ToList();
                    if (validLaps.Count > 0)
                    {
                        connection.BestLap = validLaps.Min(l => l.Laptime);
                    }
                    else if (this.currentSession.SessionType != (byte)MsgSessionInfo.SessionTypeEnum.Race)
                    {
                        // temporarily set BestLap to MaxValue for easier sorting for qualifying/practice results
                        connection.BestLap = int.MaxValue;
                    }

                    if (laps.Count > 0)
                    {
                        connection.TotalTime = (uint)laps.Sum(l => l.Laptime);
                        connection.LapCount = laps.Max(l => l.LapNo);
                        connection.Incidents += laps.Sum(l => l.Cuts);
                    }
                }

                if (this.currentSession.SessionType == (byte)MsgSessionInfo.SessionTypeEnum.Race) //if race
                {
                    ushort position = 1;

                    // compute start position
                    foreach (DriverInfo connection in this.currentSession.Drivers.Where(d => d.ConnectedTimestamp >= 0 && d.ConnectedTimestamp <= this.currentSession.Timestamp).OrderByDescending(d => d.StartSplinePos))
                    {
                        connection.StartPosition = position++;
                    }

                    foreach (DriverInfo connection in this.currentSession.Drivers.Where(d => d.ConnectedTimestamp >= 0 && d.ConnectedTimestamp > this.currentSession.Timestamp).OrderBy(d => d.ConnectedTimestamp))
                    {
                        connection.StartPosition = position++;
                    }

                    foreach (DriverInfo connection in this.currentSession.Drivers.Where(d => d.ConnectedTimestamp < 0))
                    {
                        connection.StartPosition = position++;
                    }

                    // compute end position
                    position = 1;
                    int winnerlapcount = 0;
                    uint winnertime = 0;

                    List<DriverInfo> sortedDrivers = new List<DriverInfo>(this.currentSession.Drivers.Count);

                    sortedDrivers.AddRange(this.currentSession.Drivers.Where(d => d.LapCount == currentSession.LapCount).OrderBy(GetLastLapTimestamp));
                    sortedDrivers.AddRange(this.currentSession.Drivers.Where(d => d.LapCount != currentSession.LapCount).OrderByDescending(d => d.LapCount).ThenByDescending(d => d.LastSplinePos));

                    foreach (DriverInfo connection in sortedDrivers)
                    {
                        if (position == 1)
                        {
                            winnerlapcount = connection.LapCount;
                            winnertime = connection.TotalTime;
                        }
                        connection.Position = position++;

                        if (connection.LapCount == winnerlapcount)
                        {
                            // is incorrect for players connected after race started
                            connection.Gap = FormatTimespan((int)connection.TotalTime - (int)winnertime);
                        }
                        else
                        {
                            if (winnerlapcount - connection.LapCount == 1)
                            {
                                connection.Gap = "1 lap";
                            }
                            else
                            {
                                connection.Gap = (winnerlapcount - connection.LapCount) + " laps";
                            }
                        }
                    }
                }
                else
                {
                    ushort position = 1;
                    uint winnertime = 0;
                    foreach (DriverInfo connection in this.currentSession.Drivers.OrderBy(d => d.BestLap))
                    {
                        if (position == 1)
                        {
                            winnertime = connection.BestLap;
                        }

                        connection.Position = position++;

                        if (connection.BestLap == int.MaxValue)
                        {
                            connection.BestLap = 0; // reset bestlap
                        }
                        else
                        {
                            connection.Gap = FormatTimespan((int)connection.BestLap - (int)winnertime);
                        }
                    }
                }

                if (this.currentSession.Drivers.Count > 0)
                {
                    foreach (ISessionReportHandler handler in this.SessionReportHandlers)
                    {
                        try
                        {
                            handler.HandleReport(this.currentSession);
                        }
                        catch (Exception ex)
                        {
                            this.Log(ex);
                        }
                    }
                }
            }
            finally
            {
                this.previousSession = this.currentSession;
                this.currentSession = new SessionInfo();
                this.currentSession.MaxClients = previousSession.MaxClients; // TODO can be removed when MaxClients added to MsgSessionInfo
                this.lastCarUpdateCarId = -1;

                foreach (DriverInfo connection in previousSession.Drivers)
                {
                    DriverInfo found;
                    if (this.carUsedByDictionary.TryGetValue(connection.CarId, out found) && found == connection)
                    {
                        DriverInfo recreatedConnection = new DriverInfo()
                        {
                            ConnectionId = this.currentSession.Drivers.Count(),
                            ConnectedTimestamp = found.ConnectedTimestamp,
                            DisconnectedTimestamp = found.DisconnectedTimestamp, // should be not set yet
                            DriverGuid = found.DriverGuid,
                            DriverName = found.DriverName,
                            DriverTeam = found.DriverTeam,
                            CarId = found.CarId,
                            CarModel = found.CarModel,
                            CarSkin = found.CarSkin,
                            BallastKG = found.BallastKG,
                            IsAdmin = found.IsAdmin
                        };

                        this.currentSession.Drivers.Add(recreatedConnection);
                    }
                }

                // clear the dictionary of cars currently used
                this.carUsedByDictionary.Clear();
                foreach (DriverInfo recreatedConnection in this.currentSession.Drivers)
                {
                    this.carUsedByDictionary.Add(recreatedConnection.CarId, recreatedConnection);
                }
            }
        }


        public bool IsConnected
        {
            get
            {
                return _UDP.Opened;
            }
        }

        public void Connect()
        {
            lock (lockObject)
            {
                if (IsConnected)
                {
                    throw new Exception("PluginManager already connected");
                }

                ProtocolVersion = -1;
                _UDP.Open(ListeningPort, RemoteHostname, RemotePort, MessageReceived, Log);

                try
                {
                    this.OnConnected();
                }
                catch (Exception ex)
                {
                    Log(ex);
                }

                foreach (ExternalPluginInfo externalPlugin in _externalPlugins)
                {
                    try
                    {
                        DuplexUDPClient externalPluginUdp = new DuplexUDPClient();
                        externalPluginUdp.Open(externalPlugin.ListeningPort, externalPlugin.RemoteHostname, externalPlugin.RemotePort, MessageReceivedFromExternalPlugin, Log);
                        _openExternalPlugins.Add(externalPlugin, externalPluginUdp);
                    }
                    catch (Exception ex)
                    {
                        Log(ex);
                    }
                }
            }
        }

        private void MessageReceivedFromExternalPlugin(TimestampedBytes tsb)
        {
            _UDP.Send(tsb);

            if (LogServerRequests > 0)
            {
                LogRequestToServer(AcMessageParser.Parse(tsb));
            }
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                throw new Exception("PluginManager is not connected");
            }

            _UDP.Close();

            lock (lockObject)
            {
                try
                {
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
                }
                catch (Exception ex)
                {
                    Log(ex);
                }

                try
                {
                    this.OnDisconnected();
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        private void MessageReceived(TimestampedBytes data)
        {
            lock (lockObject)
            {
                var msg = AcMessageParser.Parse(data);

                if (this.ProtocolVersion == -1 && (msg is MsgVersionInfo || msg is MsgSessionInfo))
                {
                    if (msg is MsgVersionInfo)
                    {
                        this.ProtocolVersion = ((MsgVersionInfo)msg).Version;
                    }
                    else if (msg is MsgSessionInfo)
                    {
                        this.ProtocolVersion = ((MsgSessionInfo)msg).Version;
                    }

                    if (this.ProtocolVersion != RequiredProtocolVersion && !IgnoreRequiredProtocolVersion)
                    {
                        this.Disconnect();
                        throw new Exception(string.Format("AcServer protocol version '{0}' is different from the required protocol version '{1}'. Disconnecting...",
                            this.ProtocolVersion, RequiredProtocolVersion));
                    }
                }

                try
                {
                    switch (msg.Type)
                    {
                        case ACSProtocol.MessageType.ACSP_SESSION_INFO:
                            this.OnSessionInfo((MsgSessionInfo)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_NEW_SESSION:
                            this.OnNewSession((MsgSessionInfo)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_NEW_CONNECTION:
                            this.OnNewConnection((MsgNewConnection)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_CONNECTION_CLOSED:
                            this.OnConnectionClosed((MsgConnectionClosed)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_CAR_UPDATE:
                            this.OnCarUpdate((MsgCarUpdate)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_CAR_INFO:
                            this.OnCarInfo((MsgCarInfo)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_LAP_COMPLETED:
                            this.OnLapCompleted((MsgLapCompleted)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_END_SESSION:
                            this.OnSessionEnded((MsgSessionEnded)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_CLIENT_EVENT:
                            this.OnCollision((MsgClientEvent)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_VERSION:
                            this.OnProtocolVersion((MsgVersionInfo)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_CLIENT_LOADED:
                            this.OnClientLoaded((MsgClientLoaded)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_CHAT:
                            this.OnChatMessage((MsgChat)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_ERROR:
                            this.OnServerError((MsgError)msg);
                            break;
                        case ACSProtocol.MessageType.ACSP_REALTIMEPOS_INTERVAL:
                        case ACSProtocol.MessageType.ACSP_GET_CAR_INFO:
                        case ACSProtocol.MessageType.ACSP_SEND_CHAT:
                        case ACSProtocol.MessageType.ACSP_BROADCAST_CHAT:
                        case ACSProtocol.MessageType.ACSP_GET_SESSION_INFO:
                            throw new Exception("Received unexpected MessageType (for a plugin): " + msg.Type);
                        case ACSProtocol.MessageType.ACSP_CE_COLLISION_WITH_CAR:
                        case ACSProtocol.MessageType.ACSP_CE_COLLISION_WITH_ENV:
                        case ACSProtocol.MessageType.ERROR_BYTE:
                        default:
                            throw new Exception("Received wrong or unknown MessageType: " + msg.Type);
                    }
                }
                catch (Exception ex)
                {
                    this.Log(ex);
                }

                foreach (DuplexUDPClient externalPluginUdp in _openExternalPlugins.Values)
                {
                    externalPluginUdp.TrySend(data);
                }
            }
        }

        private void OnConnected()
        {
            try
            {
                // if we do not receive the session Info in the next 3 seconds request info (async)
                ThreadPool.QueueUserWorkItem(o =>
                {
                    Thread.Sleep(3000);
                    if (this.ProtocolVersion == -1)
                    {
                        this.RequestSessionInfo(-1);
                    }
                });
            }
            catch (Exception ex)
            {
                Log(ex);
            }

            foreach (AcServerPlugin plugin in _plugins)
            {
                try
                {
                    plugin.OnConnected();
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        private void OnDisconnected()
        {
            try
            {
                this.FinalizeAndStartNewReport();
                this.currentSession.Drivers.Clear();
                this.carUsedByDictionary.Clear();
            }
            catch (Exception ex)
            {
                Log(ex);
            }

            foreach (AcServerPlugin plugin in _plugins)
            {
                try
                {
                    plugin.OnDisconnected();
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        private void OnSessionInfo(MsgSessionInfo msg)
        {
            try
            {
                bool firstSessionInfo = this.currentSession.SessionType == 0;
                this.SetSessionInfo(msg, firstSessionInfo);
                if (firstSessionInfo)
                {
                    // first time we received session info, also enable real time update
                    if (this.RealtimeUpdateInterval > 0)
                    {
                        this.EnableRealtimeReport(RealtimeUpdateInterval);
                    }
                    // request car info for all cars
                    for (int i = 0; i < this.currentSession.MaxClients; i++)
                    {
                        this.RequestCarInfo((byte)i);
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }

            foreach (AcServerPlugin plugin in _plugins)
            {
                try
                {
                    plugin.OnSessionInfo(msg);
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        private void OnNewSession(MsgSessionInfo msg)
        {
            try
            {
                this.FinalizeAndStartNewReport();
                this.currentSession.MissedSessionStart = false;
                this.SetSessionInfo(msg, true);

                if (RealtimeUpdateInterval > 0)
                {
                    this.EnableRealtimeReport(RealtimeUpdateInterval);
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }

            foreach (AcServerPlugin plugin in _plugins)
            {
                try
                {
                    plugin.OnNewSession(msg);
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        private void OnSessionEnded(MsgSessionEnded msg)
        {
            foreach (AcServerPlugin plugin in _plugins)
            {
                try
                {
                    plugin.OnSessionEnded(msg);
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        private void OnNewConnection(MsgNewConnection msg)
        {
            try
            {
                DriverInfo newConnection = new DriverInfo()
                {
                    ConnectionId = this.currentSession.Drivers.Count(),
                    DriverGuid = msg.DriverGuid,
                    DriverName = msg.DriverName,
                    DriverTeam = string.Empty, // missing in msg
                    CarId = msg.CarId,
                    CarModel = msg.CarModel,
                    CarSkin = msg.CarSkin,
                    BallastKG = 0 // missing in msg
                };

                this.currentSession.Drivers.Add(newConnection);

                DriverInfo otherDriver;
                if (this.carUsedByDictionary.TryGetValue(newConnection.CarId, out otherDriver))
                {
                    // should not happen
                    this.Log(new Exception("Car already in used by another driver"));
                    otherDriver.DisconnectedTimestamp = DateTime.UtcNow.Ticks;
                    this.carUsedByDictionary[msg.CarId] = newConnection;
                }
                else
                {
                    this.carUsedByDictionary.Add(newConnection.CarId, newConnection);
                }

                // request car info to get additional info and check when driver really is connected
                this.RequestCarInfo(msg.CarId);
            }
            catch (Exception ex)
            {
                Log(ex);
            }

            foreach (AcServerPlugin plugin in _plugins)
            {
                try
                {
                    plugin.OnNewConnection(msg);
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        private void OnConnectionClosed(MsgConnectionClosed msg)
        {
            try
            {
                DriverInfo driverReport;
                if (this.carUsedByDictionary.TryGetValue(msg.CarId, out driverReport))
                {
                    if (msg.DriverGuid == msg.DriverGuid)
                    {
                        driverReport.DisconnectedTimestamp = DateTime.UtcNow.Ticks;
                        this.carUsedByDictionary.Remove(msg.CarId);
                    }
                    else
                    {
                        this.Log(new Exception("MsgOnConnectionClosed DriverGuid does not match Guid of connected driver"));
                    }
                }
                else
                {
                    this.Log(new Exception("Car was not known to be in use"));
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }

            foreach (AcServerPlugin plugin in _plugins)
            {
                try
                {
                    plugin.OnConnectionClosed(msg);
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        private void OnCarInfo(MsgCarInfo msg)
        {
            try
            {
                DriverInfo driverReport;
                if (carUsedByDictionary.TryGetValue(msg.CarId, out driverReport))
                {
                    driverReport.CarModel = msg.CarModel;
                    driverReport.CarSkin = msg.CarSkin;
                    driverReport.DriverName = msg.DriverName;
                    driverReport.DriverTeam = msg.DriverTeam;
                    driverReport.DriverGuid = msg.DriverGuid;
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }

            foreach (AcServerPlugin plugin in _plugins)
            {
                try
                {
                    plugin.OnCarInfo(msg);
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        private void OnCarUpdate(MsgCarUpdate msg)
        {
            DriverInfo driver = null;
            try
            {
                // We check if this is the first CarUpdate message for this round (they seem to be sent in a bulk and ordered by carId)
                // If that's the case we trigger OnBulkCarUpdateFinished

                // the trick with the connectedDriversCount is used as a failsafe when single messages are received out of order
                int connectedDriversCount = this.CurrentSession.Drivers.Count(d => d.IsConnected);
                if (this.lastCarUpdateCarId - msg.CarId >= connectedDriversCount / 2)
                {
                    // Ok, this was the last one, so the last updates are like a snapshot within a milisecond or less.
                    // Great spot to examine positions, overtakes and stuff where multiple cars are compared to each other

                    this.OnBulkCarUpdateFinished();

                    // In every case we let the plugins do their calculations - before even raising the OnCarUpdate(msg). This function could
                    // take advantage of updated DriverInfos
                    foreach (AcServerPlugin plugin in _plugins)
                    {
                        try
                        {
                            plugin.OnBulkCarUpdateFinished();
                        }
                        catch (Exception ex)
                        {
                            Log(ex);
                        }
                    }
                }
                this.lastCarUpdateCarId = msg.CarId;

                // ignore updates in the first 10 seconds of the session
                if (DateTime.UtcNow.Ticks - currentSession.Timestamp > 10 * 10000000)
                {
                    driver = this.getDriverReportForCarId(msg.CarId);
                    driver.UpdatePosition(msg, this.RealtimeUpdateInterval);

                    //if (sw == null)
                    //{
                    //    sw = new StreamWriter(@"c:\workspace\positions.csv");
                    //    sw.AutoFlush = true;
                    //}
                    //sw.WriteLine(ToSingle3(msg.WorldPosition).ToString() + ", " + ToSingle3(msg.Velocity).Length());
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }

            foreach (AcServerPlugin plugin in _plugins)
            {
                try
                {
                    if (driver != null)
                        plugin.OnCarUpdate(driver);
                    plugin.OnCarUpdate(msg);
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        private void OnBulkCarUpdateFinished()
        {
            // So we'll try to compare the cars towards each other, because currently all DriverInfos
            // are up-to-date and comparable

            // First: CurrentDistanceToClosestCar
            // We'll just do a simple list of the moving cars that is ordered by the SplinePos. This doesn't respect
            // finding the position across the finish line, but this is a minor thing for now
            CurrentSession.Drivers.ForEach(x => x.CurrentDistanceToClosestCar = 0);
            var sortedDrivers = CurrentSession.Drivers.Where(x => x.CurrentSpeed > 30).OrderBy(x => x.LastSplinePos).ToArray();
            if (sortedDrivers.Length > 1)
            {
                var prev = sortedDrivers[sortedDrivers.Length - 1];
                for (int i = 0; i < sortedDrivers.Length; i++)
                {
                    var next = sortedDrivers[i];

                    var distance = (prev.LastPosition - next.LastPosition).Length();

                    if (prev.CurrentDistanceToClosestCar > distance || prev.CurrentDistanceToClosestCar == 0)
                        prev.CurrentDistanceToClosestCar = distance;

                    if (next.CurrentDistanceToClosestCar > distance || next.CurrentDistanceToClosestCar == 0)
                        next.CurrentDistanceToClosestCar = distance;

                    prev = next;
                }
            }
        }

        private void OnCollision(MsgClientEvent msg)
        {
            try
            {
                // ignore collisions in the first 5 seconds of the session
                if (DateTime.UtcNow.Ticks - currentSession.Timestamp > 5 * 10000000)
                {
                    DriverInfo driver = this.getDriverReportForCarId(msg.CarId);
                    bool withOtherCar = msg.Subtype == (byte)ACSProtocol.MessageType.ACSP_CE_COLLISION_WITH_CAR;

                    driver.Incidents += withOtherCar ? 2 : 1; // TODO only if relVel > thresh

                    DriverInfo driver2 = null;
                    if (withOtherCar && msg.OtherCarId >= 0)
                    {
                        driver2 = this.getDriverReportForCarId(msg.OtherCarId);
                        driver2.Incidents += 2; // TODO only if relVel > thresh
                    }

                    IncidentInfo incident = new IncidentInfo()
                    {
                        Type = msg.Subtype,
                        Timestamp = DateTime.UtcNow.Ticks,
                        ConnectionId1 = driver.ConnectionId,
                        ConnectionId2 = withOtherCar ? driver2.ConnectionId : -1,
                        ImpactSpeed = msg.RelativeVelocity,
                        WorldPosition = msg.WorldPosition,
                        RelPosition = msg.RelativePosition,
                    };

                    this.currentSession.Incidents.Add(incident);

                    foreach (AcServerPlugin plugin in _plugins)
                    {
                        try
                        {
                            plugin.OnCollision(incident);
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

            foreach (AcServerPlugin plugin in _plugins)
            {
                try
                {
                    plugin.OnCollision(msg);
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        private void OnLapCompleted(MsgLapCompleted msg)
        {
            try
            {
                DriverInfo driver = this.getDriverReportForCarId(msg.CarId);

                float lapLength = driver.OnLapCompleted();

                ushort position = 0;
                ushort lapNo = 0;
                for (int i = 0; i < msg.LeaderboardSize; i++)
                {
                    if (msg.Leaderboard[i].CarId == msg.CarId)
                    {
                        position = (byte)(i + 1);
                        lapNo = msg.Leaderboard[i].Laps;
                        break;
                    }
                }

                if (!this.currentSession.MissedSessionStart && this.currentSession.SessionType == (byte)MsgSessionInfo.SessionTypeEnum.Race)
                {
                    // for race compute Position based on own info (better with disconnected drivers)
                    position = (ushort)(this.currentSession.Laps.Count(l => l.LapNo == lapNo) + 1);
                }

                LapInfo lap = new LapInfo()
                {
                    ConnectionId = driver.ConnectionId,
                    Timestamp = DateTime.UtcNow.Ticks,
                    Laptime = msg.Laptime,
                    LapLength = lapLength,
                    LapNo = lapNo,
                    Position = position,
                    Cuts = msg.Cuts,
                    GripLevel = msg.GripLevel
                };

                this.currentSession.Laps.Add(lap);

                foreach (AcServerPlugin plugin in _plugins)
                {
                    try
                    {
                        plugin.OnLapCompleted(lap);
                    }
                    catch (Exception ex)
                    {
                        Log(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }

            foreach (AcServerPlugin plugin in _plugins)
            {
                try
                {
                    plugin.OnLapCompleted(msg);
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        private void OnClientLoaded(MsgClientLoaded msg)
        {
            foreach (AcServerPlugin plugin in _plugins)
            {
                try
                {
                    plugin.OnClientLoaded(msg);
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        private void OnChatMessage(MsgChat msg)
        {
            try
            {
                DriverInfo driver;
                if (this.TryGetDriverInfo(msg.CarId, out driver))
                {
                    if (!driver.IsAdmin && !string.IsNullOrWhiteSpace(AdminPassword)
                        && msg.Message.StartsWith("/admin ", StringComparison.InvariantCultureIgnoreCase))
                    {
                        driver.IsAdmin = msg.Message.Substring("/admin ".Length).Equals(AdminPassword);
                    }

                    if (driver.IsAdmin)
                    {
                        if (msg.Message.StartsWith("/send_chat ", StringComparison.InvariantCultureIgnoreCase))
                        {
                            int carIdStartIdx = "/send_chat ".Length;
                            int carIdEndIdx = msg.Message.IndexOf(' ', carIdStartIdx);
                            byte carId;
                            if (carIdEndIdx > carIdStartIdx && byte.TryParse(msg.Message.Substring(carIdStartIdx, carIdEndIdx - carIdStartIdx), out carId))
                            {
                                string chatMsg = msg.Message.Substring(carIdEndIdx);
                                SendChatMessage(carId, chatMsg);
                            }
                            else
                            {
                                SendChatMessage(msg.CarId, "Invalid car id provided");
                            }
                        }
                        else if (msg.Message.StartsWith("/broadcast ", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string broadcastMsg = msg.Message.Substring("/broadcast ".Length);
                            BroadcastChatMessage(broadcastMsg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }

            foreach (AcServerPlugin plugin in _plugins)
            {
                try
                {
                    plugin.OnChatMessage(msg);
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        private void OnProtocolVersion(MsgVersionInfo msg)
        {
            foreach (AcServerPlugin plugin in _plugins)
            {
                try
                {
                    plugin.OnProtocolVersion(msg);
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        private void OnServerError(MsgError msg)
        {
            try
            {
                if (this.LogServerErrors > 0)
                {
                    this.Log("ServerError: " + msg.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }

            foreach (AcServerPlugin plugin in _plugins)
            {
                try
                {
                    plugin.OnServerError(msg);
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        public void ProcessEnteredCommand(string cmd)
        {
            lock (lockObject)
            {
                foreach (AcServerPlugin plugin in _plugins)
                {
                    try
                    {
                        if (!plugin.OnCommandEntered(cmd))
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
        }

        #region Requests to the AcServer

        public void RequestCarInfo(byte carId)
        {
            var carInfoRequest = new RequestCarInfo() { CarId = carId };
            _UDP.Send(carInfoRequest.ToBinary());
            if (LogServerRequests > 0)
            {
                LogRequestToServer(carInfoRequest);
            }
        }

        public void BroadcastChatMessage(string msg)
        {
            var chatRequest = new RequestBroadcastChat() { ChatMessage = msg };
            _UDP.Send(chatRequest.ToBinary());
            if (LogServerRequests > 0)
            {
                LogRequestToServer(chatRequest);
            }
        }

        public void SendChatMessage(byte car_id, string msg)
        {
            var chatRequest = new RequestSendChat() { CarId = car_id, ChatMessage = msg };
            _UDP.Send(chatRequest.ToBinary());
            if (LogServerRequests > 0)
            {
                LogRequestToServer(chatRequest);
            }
        }

        public void EnableRealtimeReport(UInt16 interval)
        {
            this.RealtimeUpdateInterval = interval;
            this.currentSession.RealtimeUpdateInterval = interval;

            var enableRealtimeReportRequest = new RequestRealtimeInfo { Interval = interval };
            _UDP.Send(enableRealtimeReportRequest.ToBinary());
            if (LogServerRequests > 0)
            {
                LogRequestToServer(enableRealtimeReportRequest);
            }
        }

        /// <summary>
        /// Request a SessionInfo object, use -1 for the current session
        /// </summary>
        /// <param name="sessionIndex"></param>
        public void RequestSessionInfo(Int16 sessionIndex)
        {
            var sessionRequest = new RequestSessionInfo() { SessionIndex = sessionIndex };
            _UDP.Send(sessionRequest.ToBinary());
            if (LogServerRequests > 0)
            {
                LogRequestToServer(sessionRequest);
            }
        }

        public void RequestKickDriverById(byte car_id)
        {
            var kickRequest = new RequestKickUser() { CarId = car_id };
            _UDP.Send(kickRequest.ToBinary());
            if (LogServerRequests > 0)
            {
                LogRequestToServer(kickRequest);
            }
        }

        public void RequestSetSession(RequestSetSession requestSetSession)
        {
            _UDP.Send(requestSetSession.ToBinary());
            if (LogServerRequests > 0)
            {
                LogRequestToServer(requestSetSession);
            }
        }

        #endregion

        #region for convenience ILog is implemented by plugin manager

        public void Log(string message)
        {
            Logger.Log(message);
        }

        public void Log(Exception ex)
        {
            Logger.Log(ex);
        }

        #endregion

        #region some helper methods
        public static string FormatTimespan(int timespan)
        {
            int minutes = timespan / 1000 / 60;
            double seconds = (timespan - minutes * 1000 * 60) / 1000.0;
            return string.Format("{0:00}:{1:00.000}", minutes, seconds);
        }

        private long GetLastLapTimestamp(DriverInfo driver)
        {
            LapInfo lapReport = this.currentSession.Laps.FirstOrDefault(l => l.ConnectionId == driver.ConnectionId && l.LapNo == driver.LapCount);
            if (lapReport != null)
            {
                return lapReport.Timestamp;
            }
            return long.MaxValue;
        }
        #endregion

        private void LogRequestToServer(PluginMessage msg)
        {
            Log("Sent Request: " + msg);
        }
    }
}
