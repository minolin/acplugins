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
    public abstract class AcServerPlugin : AcServerPluginBase
    {
        public AcServerPluginManager PluginManager { get; private set; }

        public IConfigManager Config { get; private set; }

        protected internal byte[] _fingerprint;

        #region Cache and Helpers

        private ConcurrentDictionary<int, MsgCarInfo> _CarInfo = null;
        public IDictionary<int, MsgCarInfo> CarInfo { get { return _CarInfo; } }

        #endregion

        protected AcServerPlugin(string pluginName = null)
            : base(pluginName)
        {
        }

        #region sealed overrides of BaseAcServerPlugin methods - usually a call of base.EventHandler(), but this one is more secure

        protected internal sealed override void OnInitBase(AcServerPluginManager manager)
        {
            PluginManager = manager;
            Config = manager.Config;
            _CarInfo = new ConcurrentDictionary<int, MsgCarInfo>(10, 64);
            _fingerprint = Hash(Config.GetSetting("ac_server_directory") + PluginManager.RemotePort);
            OnInit();
        }

        protected internal sealed override void OnConnectedBase()
        {
            OnConnected();
        }

        protected internal sealed override void OnDisconnectedBase()
        {
            OnDisconnected();
        }

        /// <summary>
        /// Called when a command was entered.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <returns>Whether the command should be passed to the next plugin.</returns>
        protected internal sealed override bool OnCommandEnteredBase(string cmd)
        {
            return OnCommandEntered(cmd);
        }

        protected internal sealed override void OnNewSessionBase(MsgNewSession msg)
        {
            CarInfo.Clear();
            for (byte i = 0; i < PluginManager.MaxClients; i++)
            {
                PluginManager.RequestCarInfo(i);
            }
            OnNewSession(msg);
        }

        protected internal sealed override void OnCarInfoBase(MsgCarInfo msg)
        {
            _CarInfo.AddOrUpdate(msg.CarId, msg, (key, val) => val);
            OnCarInfo(msg);
        }

        protected internal sealed override void OnNewConnectionBase(MsgNewConnection msg)
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

        protected internal sealed override void OnConnectionClosedBase(MsgConnectionClosed msg)
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

        protected internal sealed override void OnSessionEndedBase(MsgSessionEnded msg)
        {
            OnSessionEnded(msg);
        }

        protected internal sealed override void OnCarUpdateBase(MsgCarUpdate msg)
        {
            OnCarUpdate(msg);
        }

        protected internal sealed override void OnLapCompletedBase(MsgLapCompleted msg)
        {
            OnLapCompleted(msg);
        }

        protected internal sealed override void OnCollisionBase(MsgClientEvent msg)
        {
            OnCollision(msg);
        }

        #endregion

        #region overridable event handlers

        /// <summary>
        /// Called when a command was entered.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <returns>Whether the command should be passed to the next plugin.</returns>
        public virtual bool OnCommandEntered(string cmd)
        {
#pragma warning disable 618
            OnConsoleCommand(cmd); // obviously remove these lines when workarounds are no longer needed
#pragma warning restore 618
            return true;
        }

        public virtual void OnInit() { }
        public virtual void OnConnected() { }
        public virtual void OnDisconnected() { }

        public virtual void OnNewSession(MsgNewSession msg) { }
        public virtual void OnSessionEnded(MsgSessionEnded msg) { }
        public virtual void OnConnectionClosed(MsgConnectionClosed msg) { }
        public virtual void OnNewConnection(MsgNewConnection msg) { }
        public virtual void OnCarInfo(MsgCarInfo msg) { }
        public virtual void OnCarUpdate(MsgCarUpdate msg) { }
        public virtual void OnLapCompleted(MsgLapCompleted msg) { }
        public virtual void OnCollision(MsgClientEvent msg) { }

        #endregion

        #region workarounds so that plugins developed with old AcServerPlugin are compiling

        [Obsolete("Use PluginManager.BroadcastChatMessage instead")]
        protected internal void BroadcastChatMessage(string msg)
        {
            this.PluginManager.BroadcastChatMessage(msg);
        }

        [Obsolete("Use PluginManager.SendChatMessage instead")]
        protected internal void SendChatMessage(byte car_id, string msg)
        {
            this.PluginManager.SendChatMessage(car_id, msg);
        }

        [Obsolete("Use PluginManager.EnableRealtimeReport instead")]
        protected internal void EnableRealtimeReport(UInt16 interval)
        {
            this.PluginManager.EnableRealtimeReport(interval);
        }

        [Obsolete("Only for compatibility with plugins developed for old AcServerPlugin, override OnCommandEntered instead")]
        public virtual void OnConsoleCommand(string cmd) { }

        [Obsolete("Use PluginManager.ServerName instead")]
        public string Servername { get { return PluginManager.ServerName; } }

        [Obsolete("Use PluginManager.Track instead")]
        public string Track { get { return PluginManager.Track; } }

        [Obsolete("Use PluginManager.TrackLayout instead")]
        public string TrackLayout { get { return PluginManager.TrackLayout; } }

        [Obsolete("Use PluginManager.MaxClients instead")]
        public int MaxClients { get { return PluginManager.MaxClients; } }

        #endregion
    }
}
