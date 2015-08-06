using acPlugins4net.configuration;
using acPlugins4net.helpers;
using acPlugins4net.messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net
{
    [Obsolete("This should be replaced by AcServerPluginNew, rename AcServerPluginNew to AcServerPlugin")]
    public abstract class AcServerPlugin : MD5Hashable
    {
        private readonly DuplexUDPClient _UDP;
        public IConfigManager Config { get; private set; }
        private WorkaroundHelper _Workarounds = null;
        private ILog _log = null;
        protected internal byte[] _fingerprint = null;

        public string PluginName { get; set; }

        #region Cache and Helpers

        public string Servername { get; private set; }
        public string Track { get; private set; }
        public string TrackLayout { get; private set; }
        public int MaxClients { get; private set; }

        private ConcurrentDictionary<int, MsgCarInfo> _CarInfo = null;
        public IDictionary<int, MsgCarInfo> CarInfo { get { return _CarInfo; } }


        #endregion

        public AcServerPlugin()
        {
            _UDP = new DuplexUDPClient();
            PluginName = "Unnamed plugin";
            _log = new ConsoleLogger("minoratingplugin.txt");
            Config = new AppConfigConfigurator();
            _Workarounds = new WorkaroundHelper(Config);
            _CarInfo = new ConcurrentDictionary<int, MsgCarInfo>(10, 64);
            _fingerprint = Hash(Config.GetSetting("ac_server_directory") + Config.GetSetting("acServer_port"));
        }

        public void RunUntilAborted()
        {
            _log.Log(PluginName + " starting up...");
            Init();
            _log.Log("Initialized, start UDP connection...");
            Connect();
            _log.Log("... ok, we're good to go.");

            var input = Console.ReadLine();
            while (input != "x" && input != "exit")
            {
                // Basically we're blocking the Main Thread until exit.
                // Ugly, but pretty easy to use by the deriving Plugin

                // To have a bit of functionality we'll let the server admin 
                // type in commands that can be understood by the deriving plugin
                if (!string.IsNullOrEmpty(input))
                    OnConsoleCommand(input);

                input = Console.ReadLine();
            }

            Disconnect();
        }

        private void Init()
        {
            Servername = _Workarounds.FindServerConfigEntry("NAME=");
            Track = _Workarounds.FindServerConfigEntry("TRACK=");
            TrackLayout = _Workarounds.FindServerConfigEntry("CONFIG_TRACK=");
            _log.Log("Track/Layout is " + Track + "[" + TrackLayout + "] (by workaround)");
            MaxClients = Convert.ToInt32(_Workarounds.FindServerConfigEntry("MAX_CLIENTS="));

            OnInit();
        }

        public virtual void Connect()
        {
            // First we're getting the configured ports (read directly from the server_config.ini)
            string acServerPortString = _Workarounds.FindServerConfigEntry("UDP_PLUGIN_LOCAL_PORT=");
            string pluginPortString = _Workarounds.FindServerConfigEntry("UDP_PLUGIN_ADDRESS=");

            #region determine the acServerPort with helpful error messages - this *will* be done wrong
            if (string.IsNullOrWhiteSpace(acServerPortString) || acServerPortString == "0")
                throw new Exception("There is no UDP_PLUGIN_LOCAL_PORT defined in the server_config.ini - check the file and the path in the <plugin>.exe.config");

            int acServerPort;
            if(!int.TryParse(acServerPortString, out acServerPort))
                throw new Exception("Error in server_config.ini: UDP_PLUGIN_LOCAL_PORT=" + acServerPortString + " is not a valid port - check the file and the path in the <plugin>.exe.config");
            #endregion

            #region the same for the plugin port - including a restriction to localhost (see http://www.assettocorsa.net/forum/index.php?threads/about-that-maybe-server-api.24360/page-8#post-507070)
            if (string.IsNullOrWhiteSpace(pluginPortString))
                throw new Exception("There is no UDP_PLUGIN_ADDRESS defined in the server_config.ini - check the file and the path in the <plugin>.exe.config");

            if(!pluginPortString.StartsWith("127.0.0.1:"))
                throw new Exception("The UDP_PLUGIN_ADDRESS (defined in the server_config.ini) must referenced locally, that is 127.0.0.1:<port> - check the file and the path in the <plugin>.exe.config");

            int pluginPort;
            if(!int.TryParse(pluginPortString.Replace("127.0.0.1:", ""), out pluginPort))
                throw new Exception("Error in server_config.ini: UDP_PLUGIN_ADDRESS=" + pluginPortString + " is not a valid port - check the file and the path in the <plugin>.exe.config");
            #endregion

            _UDP.Open(pluginPort, "127.0.0.1", acServerPort, MessageReceived, OnError);
        }

        public virtual void Disconnect()
        {
            _UDP.Close();
        }

        protected virtual void OnError(Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }

        private void MessageReceived(byte[] data)
        {
            AcMessageParser.Activate(this, data);
        }

        #region base event handlers - usually a call of base.EventHandler(), but this one is more secure

        internal void OnNewSessionBase(MsgNewSession msg)
        {
            CarInfo.Clear();
            for (byte i = 0; i < MaxClients; i++)
            {
                _UDP.TrySend(new RequestCarInfo() { CarId = i }.ToBinary());
            }
            OnNewSession(msg);
        }

        internal void OnCarInfoBase(MsgCarInfo msg)
        {
            _CarInfo.AddOrUpdate(msg.CarId, msg, (key, val) => val);
            OnCarInfo(msg);
        }

        internal void OnNewConnectionBase(MsgNewConnection msg)
        {
            var carInfo = new MsgCarInfo()
            {
                CarId = msg.CarId,
                CarModel = msg.CarModel,
                CarSkin = msg.CarSkin,
                DriverGuid = msg.DriverGuid,
                DriverName = msg.DriverName,
                IsConnected = true,
            };
            _CarInfo.AddOrUpdate(msg.CarId, carInfo, (key, val) => val);
            OnNewConnection(msg);
        }

        internal void OnConnectionClosedBase(MsgConnectionClosed msg)
        {
            var carInfo = new MsgCarInfo()
            {
                CarId = msg.CarId,
                CarModel = msg.CarModel,
                CarSkin = msg.CarSkin,
                DriverGuid = string.Empty,
                DriverName = string.Empty,
                IsConnected = false,
            };
            _CarInfo.AddOrUpdate(msg.CarId, carInfo, (key, val) => val);
            OnConnectionClosed(msg);
        }


        #endregion

        #region overridable event handlers

        public virtual void OnInit() { }
        public virtual void OnConsoleCommand(string cmd) { }
        public virtual void OnNewSession(MsgNewSession msg) { }
        public virtual void OnSessionEnded(MsgSessionEnded msg) { }
        public virtual void OnConnectionClosed(MsgConnectionClosed msg) { }
        public virtual void OnNewConnection(MsgNewConnection msg) { }
        public virtual void OnCarInfo(MsgCarInfo msg) { }
        public virtual void OnCarUpdate(MsgCarUpdate msg) { }
        public virtual void OnLapCompleted(MsgLapCompleted msg) { }
        public virtual void OnCollision(MsgClientEvent msg) { }

        #endregion

        #region Requests to the AcServer

        protected internal void BroadcastChatMessage(string msg)
        {
            var chatRequest = new RequestBroadcastChat() { ChatMessage = msg };
            _UDP.Send(chatRequest.ToBinary());
            Console.WriteLine("Broadcasted " + chatRequest.ToString());
        }

        protected internal void SendChatMessage(byte car_id, string msg)
        {
            var chatRequest = new RequestSendChat() { CarId = car_id, ChatMessage = msg };
            _UDP.Send(chatRequest.ToBinary());
            Console.WriteLine("Broadcasted " + chatRequest.ToString());
        }

        protected internal void EnableRealtimeReport(UInt16 interval)
        {
            var enableRealtimeReportRequest = new RequestRealtimeInfo { Interval = interval };
            _UDP.Send(enableRealtimeReportRequest.ToBinary());
            Console.WriteLine("Realtime pos interval now set to: {0} ms", interval);
        }

        #endregion
    }
}
