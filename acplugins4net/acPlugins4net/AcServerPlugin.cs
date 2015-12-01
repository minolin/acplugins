using System;
using acPlugins4net.helpers;
using acPlugins4net.messages;
using acPlugins4net.info;

namespace acPlugins4net
{
    public abstract class AcServerPlugin
    {
        public AcServerPluginManager PluginManager { get; private set; }

        protected AcServerPlugin(string pluginName = null)
        {
            PluginName = pluginName ?? GetType().Name;
        }

        public string PluginName
        {
            get;
            protected set;
        }

        internal void Initialize(AcServerPluginManager manager)
        {
            this.PluginManager = manager;
            this.OnInit();
        }

        protected internal virtual void OnInit() { }

        protected internal virtual void OnConnected() { }

        protected internal virtual void OnDisconnected() { }

        protected internal virtual bool OnCommandEntered(string cmd) { return true; }

        #region handlers for raw acServer messages
        protected internal virtual void OnSessionInfo(MsgSessionInfo msg) { }

        protected internal virtual void OnNewSession(MsgSessionInfo msg) { }

        protected internal virtual void OnSessionEnded(MsgSessionEnded msg) { }

        protected internal virtual void OnNewConnection(MsgNewConnection msg) { }

        protected internal virtual void OnConnectionClosed(MsgConnectionClosed msg) { }

        protected internal virtual void OnCarInfo(MsgCarInfo msg) { }

        protected internal virtual void OnCarUpdate(MsgCarUpdate msg) { }

        protected internal virtual void OnCollision(MsgClientEvent msg) { }

        protected internal virtual void OnLapCompleted(MsgLapCompleted msg) { }

        protected internal virtual void OnClientLoaded(MsgClientLoaded msg) { }

        protected internal virtual void OnChatMessage(MsgChat msg) { }

        protected internal virtual void OnProtocolVersion(MsgVersionInfo msg) { }

        protected internal virtual void OnServerError(MsgError msg) { }
        #endregion event handlers for raw acServer messages

        #region handlers for events refined by PluginManager
        /// <summary>
        /// This is triggered after all realtime reports per interval have arrived - they are now
        /// up-to-date and can be accessed via the DriverInfo mechanics
        /// </summary>
        protected internal virtual void OnBulkCarUpdateFinished() { }

        protected internal virtual void OnLapCompleted(LapInfo msg) { }

        protected internal virtual void OnCollision(IncidentInfo msg) { }

        protected internal virtual void OnCarUpdate(DriverInfo driverInfo) { }

        protected internal virtual void OnAcServerTimeout() { }

        #endregion handlers for events refined by PluginManager
    }
}
