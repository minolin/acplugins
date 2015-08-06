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
    public abstract class AcServerPluginNew : MD5Hashable, IAcServerPlugin
    {
        public string PluginName { get; set; }

        protected AcServerPluginManager pluginManager { get; private set; }

        public IConfigManager Config { get; private set; }
        protected internal byte[] _fingerprint = null;

        #region Cache and Helpers

        private ConcurrentDictionary<int, MsgCarInfo> _CarInfo = null;
        public IDictionary<int, MsgCarInfo> CarInfo { get { return _CarInfo; } }

        #endregion

        public AcServerPluginNew()
        {
            PluginName = "Unnamed plugin";
        }

        #region explicit implementations of IAcServerPlugin interface - usually a call of base.EventHandler(), but this one is more secure

        void IAcServerPlugin.OnInit(AcServerPluginManager manager)
        {
            pluginManager = manager;
            Config = manager.Config;
            _CarInfo = new ConcurrentDictionary<int, MsgCarInfo>(10, 64);
            _fingerprint = Hash(Config.GetSetting("ac_server_directory") + Config.GetSetting("acServer_port"));
            manager.Log.Log("Track/Layout is " + manager.Track + "[" + manager.TrackLayout + "] (by workaround)");
            OnInit(manager);
        }

        void IAcServerPlugin.OnConnected()
        {
            OnConnected();
        }

        void IAcServerPlugin.OnDisconnected()
        {
            OnDisconnected();
        }

        /// <summary>
        /// Called when a console command was entered.
        /// </summary>
        /// <param name="cmd">The console command.</param>
        /// <returns>Whether the command should be send to the next plugin.</returns>
        bool IAcServerPlugin.OnConsoleCommand(string cmd)
        {
            return OnConsoleCommand(cmd);
        }

        void IAcServerPlugin.OnNewSession(MsgNewSession msg)
        {
            CarInfo.Clear();
            for (byte i = 0; i < pluginManager.MaxClients; i++)
            {
                pluginManager.RequestCarInfo(i);
            }
            OnNewSession(msg);
        }

        void IAcServerPlugin.OnCarInfo(MsgCarInfo msg)
        {
            _CarInfo.AddOrUpdate(msg.CarId, msg, (key, val) => val);
            OnCarInfo(msg);
        }

        void IAcServerPlugin.OnNewConnection(MsgNewConnection msg)
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

        void IAcServerPlugin.OnConnectionClosed(MsgConnectionClosed msg)
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

        void IAcServerPlugin.OnSessionEnded(MsgSessionEnded msg)
        {
            OnSessionEnded(msg);
        }

        void IAcServerPlugin.OnCarUpdate(MsgCarUpdate msg)
        {
            OnCarUpdate(msg);
        }

        void IAcServerPlugin.OnLapCompleted(MsgLapCompleted msg)
        {
            OnLapCompleted(msg);
        }

        void IAcServerPlugin.OnCollision(MsgClientEvent msg)
        {
            OnCollision(msg);
        }

        #endregion

        #region overridable event handlers

        /// <summary>
        /// Called when a console command was entered.
        /// </summary>
        /// <param name="cmd">The console command.</param>
        /// <returns>Whether the command should be send to the next plugin.</returns>
        public virtual bool OnConsoleCommand(string cmd) { return true; }

        public virtual void OnInit(AcServerPluginManager manager) { }
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

        #region Requests to the AcServer (might be removed because they are implemented in AcPluginManager (without Console.WriteLine))

        protected internal void BroadcastChatMessage(string msg)
        {
            this.pluginManager.BroadcastChatMessage(msg);
            Console.WriteLine("Broadcasted '{0}'", msg);
        }

        protected internal void SendChatMessage(byte car_id, string msg)
        {
            this.pluginManager.SendChatMessage(car_id, msg);
            Console.WriteLine("Sent chat message '{1}' to car {0}", car_id, msg);
        }

        protected internal void EnableRealtimeReport(UInt16 interval)
        {
            this.pluginManager.EnableRealtimeReport(interval);
            Console.WriteLine("Realtime pos interval now set to: {0} ms", interval);
        }

        #endregion
    }
}
