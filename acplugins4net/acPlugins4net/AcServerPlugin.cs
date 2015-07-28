using acPlugins4net.configuration;
using acPlugins4net.helpers;
using acPlugins4net.messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net
{
    public abstract class AcServerPlugin : MD5Hashable
    {
        private DuplexUDPClient _UDP = null;
        public IConfigManager Config { get; private set; }
        private WorkaroundHelper _Workarounds = null;
        private ILog _log = null;
        protected internal byte[] _fingerprint = null;

        #region Cache and Helpers

        public string Track { get; private set; }
        public string TrackLayout { get; private set; }

        public Dictionary<int, MsgCarInfo> CarInfo { get; set; }

        #endregion

        public AcServerPlugin()
        {
            _log = new ConsoleLogger();
            Config = new AppConfigConfigurator();
            _Workarounds = new WorkaroundHelper(Config);
            _fingerprint = Hash(Config.GetSetting("ac_server_directory") + Config.GetSetting("acServer_port"));
        }

        public void RunUntilAborted()
        {
            _log.Log("AcServerPlugin starting up...");
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
                if(!string.IsNullOrEmpty(input))
                    OnConsoleCommand(input);

                input = Console.ReadLine();
            }
        }

        private void Init()
        {
#if DEBUG
            Track = "mugello";
            TrackLayout = "mugello";
#else
            Track = _Workarounds.FindServerConfigEntry("TRACK=");
            TrackLayout = _Workarounds.FindServerConfigEntry("CONFIG_TRACK=");
            _log.Log("Track/Layout is " + Track + "[" + TrackLayout + "] (by workaround)");
#endif
            OnInit();
        }

        private void Connect()
        {
            // First we're getting the configured ports (app.config)
            var acServerPort = Config.GetSettingAsInt("acServer_port");
            var pluginPort = Config.GetSettingAsInt("plugin_port");

            _UDP = new DuplexUDPClient();
            _UDP.Open(pluginPort, acServerPort, MessageReceived, OnError);
        }

        protected virtual void OnError(Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }

        private void MessageReceived(byte[] data)
        {
            AcMessageParser.Activate(this, data);
        }

        #region overridable event handlers

        public virtual void OnInit() { }
        public virtual void OnConsoleCommand(string cmd) { }
        public virtual void OnNewSession(MsgNewSession msg) { }
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
            _UDP.TrySend(chatRequest.ToBinary());
            Console.WriteLine("Broadcasted " + chatRequest.ToString());
        }

        #endregion
    }
}
