using acPlugins4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using acPlugins4net.messages;
using MinoRatingPlugin.minoRatingServer;
using System.Threading;
using acPlugins4net.kunos;
using acPlugins4net.helpers;
using System.ServiceModel;

namespace MinoRatingPlugin
{
    public class MinoratingPlugin : AcServerPlugin
    {
        public LiveDataDumpClient LiveDataServer { get; set; }
        public string TrustToken { get; set; }
        public Guid CurrentSessionGuid { get; set; }
        public static Version PluginVersion = new Version(0, 4, 0);

        static void Main(string[] args)
        {
            try
            {
                AcServerPluginManager pluginManager = new AcServerPluginManager(new FileLogWriter("log", "minoplugin.txt") { CopyToConsole = true, LogWithTimestamp = true });
                pluginManager.LoadInfoFromServerConfig();
                pluginManager.AddPlugin(new MinoratingPlugin());
                pluginManager.LoadPluginsFromAppConfig();
                RunPluginInConsole.RunUntilAborted(pluginManager);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
        }
        }

        public override void OnInit()
        {
#if DEBUG
            LiveDataServer = new LiveDataDumpClient(new BasicHttpBinding(), new EndpointAddress("http://localhost:806/minorating"));
#else
            LiveDataServer = new LiveDataDumpClient(new BasicHttpBinding(), new EndpointAddress("http://plugin.minorating.com:806/minorating"));
#endif

            TrustToken = Config.GetSetting("server_trust_token");
            if (string.IsNullOrEmpty(TrustToken))
            {
                TrustToken = Guid.NewGuid().ToString();
                Config.SetSetting("server_trust_token", TrustToken);
            }
            CurrentSessionGuid = Guid.Empty;

            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    PluginManager.Log("Plugin Version " + PluginVersion);
                    var serverVersion = LiveDataServer.GetVersion();
                    PluginManager.Log("Connection to server with version: " + serverVersion);

                    if (serverVersion > PluginVersion)
                    {
                        PluginManager.Log("================================");
                        PluginManager.Log("================================");
                        PluginManager.Log("Version mismatch, your plugin seems to be outdated. Please consider downloading a new one from the forums");
                        PluginManager.Log("For the moment we'll do our best and try to go on.");
                        PluginManager.Log("================================");
                    }
                }
                catch (Exception ex)
                {
                    PluginManager.Log("Error connecting to the remote server :(");
                    PluginManager.Log(ex);
                }
            });
        }

        public override void OnSessionInfo(MsgSessionInfo msg)
        {
            if(msg.Type == ACSProtocol.MessageType.ACSP_NEW_SESSION || CurrentSessionGuid == Guid.Empty)
                OnNewSession(msg);
        }

        public override void OnNewSession(MsgSessionInfo msg)
        {
            PluginManager.Log("===============================");
            PluginManager.Log("===============================");
            PluginManager.Log("OnNewSession: " + msg.Name + "@" + msg.ServerName);
            PluginManager.Log("===============================");
            PluginManager.Log("===============================");
            CurrentSessionGuid = LiveDataServer.NewSession(CurrentSessionGuid, msg.ServerName, msg.Track + "[" + msg.TrackConfig + "]", msg.SessionType, msg.Laps, msg.WaitTime, msg.SessionDuration, msg.AmbientTemp, msg.RoadTemp, TrustToken, _fingerprint, PluginVersion);
        }

        public override void OnNewConnection(MsgNewConnection msg)
        {
            PluginManager.Log("OnNewConnection: " + msg.DriverName + "@" + msg.CarModel);
            HandleClientActions(LiveDataServer.ClientConnected(CurrentSessionGuid, msg.CarId, msg.CarModel, msg.DriverName, msg.DriverGuid));
        }

        public override void OnSessionEnded(MsgSessionEnded msg)
        {
            PluginManager.Log("Session ended");
            HandleClientActions(LiveDataServer.EndSession(CurrentSessionGuid));
        }

        public override void OnConnectionClosed(MsgConnectionClosed msg)
        {
            PluginManager.Log("OnConnectionClosed: " + msg.DriverName + "@" + msg.CarModel);
            HandleClientActions(LiveDataServer.ClientDisconnected(CurrentSessionGuid, msg.CarId, msg.CarModel, msg.CarSkin, msg.DriverGuid));
        }

        public override void OnLapCompleted(MsgLapCompleted msg)
        {
            MsgCarInfo driver;
            if (!CarInfo.TryGetValue(msg.CarId, out driver))
                PluginManager.Log("Error; car_id " + msg.CarId + " was not known by the CarInfo Dictionary :(");
            else
            {
                PluginManager.Log("LapCompleted by " + driver.DriverName + ": " + TimeSpan.FromMilliseconds(msg.Laptime));
                HandleClientActions(LiveDataServer.LapCompleted(CurrentSessionGuid, msg.CarId, driver.DriverGuid, msg.Laptime, msg.Cuts, msg.GripLevel, ConvertLB(msg.Leaderboard)));
            }
        }

        private List<CollisionBag> contactTrees = new List<CollisionBag>();

        public override void OnCollision(MsgClientEvent msg)
        {
            if (msg.Subtype == (byte)ACSProtocol.MessageType.ACSP_CE_COLLISION_WITH_CAR)
            {
                // TODO: Messy code. Needs rewrite as soon as I know where I'm heading.
                // We'll check if the contact partners are part of an contact tree
                bool partOfATree = false;
                lock (contactTrees)
                {
                    foreach (var ct in contactTrees)
                    {
                        // If both can't be put into the contact tree, we'll treat this as new
                        if (ct.TryAdd(msg.CarId, msg.OtherCarId))
                        {
                            partOfATree = true;
                            break;
                        }
                    }
                    PluginManager.Log("" + DateTime.Now.TimeOfDay + " OnCollision (" + msg.CarId + "vs" + msg.OtherCarId + "), contantTrees.Count=" + contactTrees.Count + ", partOfATree=" + partOfATree);

                    if (!partOfATree)
                    {
                        // Then we'll start a new one
                        contactTrees.Add(CollisionBag.StartNew(msg.CarId, msg.OtherCarId, EvaluateContactTree, PluginManager));
                    }
                }

                HandleClientActions(LiveDataServer.Collision(CurrentSessionGuid, msg.CarId, msg.OtherCarId, msg.RelativeVelocity, 0.667234f, msg.RelativePosition.x, msg.RelativePosition.z, msg.WorldPosition.x, msg.WorldPosition.z));
            }
            else
            {
                PluginManager.Log("Collision occured!!! " + msg.CarId + " vs. wall");
                HandleClientActions(LiveDataServer.Collision(CurrentSessionGuid, msg.CarId, -1, msg.RelativeVelocity, 0.667234f, msg.RelativePosition.x, msg.RelativePosition.z, msg.WorldPosition.x, msg.WorldPosition.z));
            }
        }

        private void EvaluateContactTree(CollisionBag bag)
        {
            lock (contactTrees)
                contactTrees.Remove(bag);
            HandleClientActions(LiveDataServer.CollisionTreeEnded(CurrentSessionGuid, bag.First, bag.Second, bag.Count, bag.Started, bag.LastCollision));
        }

        private void HandleClientActions(PluginReaction[] actions)
        {
            if (actions == null)
                throw new ArgumentNullException("PluginReaction[] actions", "Looks like the server didn't create an empty PluginReaction array");

            foreach (var a in actions)
            {
                if (string.IsNullOrEmpty(a.Text))
                    a.Text = "";

                if (a.Delay == 0)
                {
                    ExecuteAction(a);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        Thread.Sleep(a.Delay);
                        ExecuteAction(a);
                    });
                }
            }
        }

        private void ExecuteAction(PluginReaction a)
        {
            try
            {
                PluginManager.Log("Action for car " + a.CarId + ": " + a.Reaction + " " + a.Text);
                if (a.Reaction == PluginReaction.ReactionType.Whisper)
                    PluginManager.SendChatMessage(a.CarId, a.Text);
                else if (a.Reaction == PluginReaction.ReactionType.Broadcast)
                    PluginManager.BroadcastChatMessage(a.Text);
                else if (a.Reaction == PluginReaction.ReactionType.Kick)
                {
                    // To be 100% sure we kick the right person we'll have to compare the steam id
                    MsgCarInfo c;
                    if (CarInfo.TryGetValue(a.CarId, out c))
                        if (c.IsConnected && c.DriverGuid == a.SteamId)
                        {
                            PluginManager.BroadcastChatMessage("" + c.DriverName + " has been kicked by minorating.com");
                            PluginManager.RequestKickDriverById(a.CarId);
                        }
                }
            }
            catch (Exception) { }
        }

        // We have to convert the acPlugins4net-Leaderboard to a minoRating one. This is pretty stupid mapping
        LeaderboardEntry[] ConvertLB(List<MsgLapCompletedLeaderboardEnty> leaderboard)
        {
            var array = new LeaderboardEntry[leaderboard.Count];
            for (int i = 0; i < leaderboard.Count; i++)
            {
                array[i] = new LeaderboardEntry()
                {
                    CarId = leaderboard[i].CarId,
                    DriverId = CarInfo[leaderboard[i].CarId].DriverGuid,
                    LapsDriven = leaderboard[i].Laps,
                    Time = leaderboard[i].Laptime
                };
            };

            return array;
        }

        public override void OnCarInfo(MsgCarInfo msg)
        {
            PluginManager.Log("CarInfo: " + msg.CarId + ", " + msg.DriverName + "@" + msg.CarModel);

            string driverName = msg.DriverName;
            string driverGuid = msg.DriverGuid;

            if (!msg.IsConnected)
            {
                driverName = "";
                driverGuid = "";
            }

            HandleClientActions(LiveDataServer.RandomCarInfo(CurrentSessionGuid, msg.CarId, msg.CarModel, driverName, driverGuid));
        }

        public override void OnChatMessage(MsgChat msg)
        {
            if (!msg.IsCommand)
                return;

            var split = msg.Message.Split(' ');
            if (split.Length > 0)
            {
                switch (split[0])
                {
                    case "/mr":
                    case "/minorating":
                        HandleClientActions(LiveDataServer.RequestDriverRating(CurrentSessionGuid, msg.CarId));
                        break;
                    default:
                        break;
                }
            }
        }

        public override void OnClientLoaded(MsgClientLoaded msg)
        {
            HandleClientActions(LiveDataServer.RequestDriverRating(CurrentSessionGuid, msg.CarId));
            HandleClientActions(LiveDataServer.RequestDriverLoaded(CurrentSessionGuid, msg.CarId));
        }
    }
}
