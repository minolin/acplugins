using System;
using acPlugins4net.helpers;
using acPlugins4net.messages;

namespace acPlugins4net
{
    /// <summary>
    /// This is the most basic abstract base class for an AC server plugin. It does not contain any functionality.
    /// If you want to have some basic features provided by the base class derive from AcServerPlugin instead.
    /// </summary>
    public abstract class AcServerPluginBase : MD5Hashable
    {
        protected AcServerPluginBase(string pluginName = null)
        {
            PluginName = pluginName ?? GetType().Name;
        }

        public string PluginName
        {
            get;
            protected set;
        }

        protected internal virtual void OnInitBase(AcServerPluginManager manager) { }

        protected internal virtual void OnConnectedBase() { }

        protected internal virtual void OnDisconnectedBase() { }

        protected internal virtual bool OnCommandEnteredBase(string cmd) { return true; }

        protected internal virtual void OnSessionInfoBase(MsgSessionInfo msg) { }

        protected internal virtual void OnNewSessionBase(MsgSessionInfo msg) { }

        protected internal virtual void OnSessionEndedBase(MsgSessionEnded msg) { }

        protected internal virtual void OnNewConnectionBase(MsgNewConnection msg) { }

        protected internal virtual void OnConnectionClosedBase(MsgConnectionClosed msg) { }

        protected internal virtual void OnCarInfoBase(MsgCarInfo msg) { }

        protected internal virtual void OnCarUpdateBase(MsgCarUpdate msg) { }

        protected internal virtual void OnCollisionBase(MsgClientEvent msg) { }

        protected internal virtual void OnLapCompletedBase(MsgLapCompleted msg) { }

        protected internal virtual void OnClientLoadedBase(MsgClientLoaded msg) { }

        protected internal virtual void OnChatMessageBase(MsgChat msg) { }

        internal void OnErrorBase(MsgError msg)
        {
            throw new NotImplementedException();
        }
    }
}
