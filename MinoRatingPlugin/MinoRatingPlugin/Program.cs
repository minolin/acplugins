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

namespace MinoRatingPlugin
{
    class MinoratingPlugin : AcServerPlugin
    {
        public LiveDataDumpClient LiveDataServer { get; set; }
        public string TrustToken { get; set; }
        public Guid CurrentSessionGuid { get; set; }
        public static Version PluginVersion = new Version(0, 3, 3);

        static void Main(string[] args)
        {
            RunPluginInConsole.RunSinglePluginUntilAborted(new MinoratingPlugin(), new FileLogWriter("log", "minoplugin.txt") { CopyToConsole = true, LogWithTimestamp = true });
        }

        public override void OnInit()
        {
            LiveDataServer = new LiveDataDumpClient();
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
                        PluginManager.Log("Version missmatch, your plugin seems to be outdated. Please consider downloading a new one from the forums");
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

        public override void OnNewSession(MsgSessionInfo msg)
        {
            PluginManager.Log("===============================");
            PluginManager.Log("===============================");
            PluginManager.Log("OnNewSession: " + msg.Name + "@" + PluginManager.ServerName);
            PluginManager.Log("===============================");
            PluginManager.Log("===============================");
            CurrentSessionGuid = LiveDataServer.NewSession(CurrentSessionGuid, PluginManager.ServerName, PluginManager.Track + "[" + PluginManager.TrackLayout + "]", msg.SessionType, msg.Laps, msg.WaitTime, msg.TimeOfDay, msg.AmbientTemp, msg.RoadTemp, TrustToken, _fingerprint);
        }

        public override void OnNewConnection(MsgNewConnection msg)
        {
            PluginManager.Log("OnNewConnection: " + msg.DriverName + "@" + msg.CarModel);
            LiveDataServer.NewConnection(CurrentSessionGuid, msg.CarId, msg.CarModel, msg.DriverName, msg.DriverGuid, TrustToken);
        }

        public override void OnSessionEnded(MsgSessionEnded msg)
        {
            LiveDataServer.EndSession(CurrentSessionGuid, TrustToken, _fingerprint);
            PluginManager.Log("Session ended");
        }

        public override void OnConnectionClosed(MsgConnectionClosed msg)
        {
            PluginManager.Log("OnConnectionClosed: " + msg.DriverName + "@" + msg.CarModel);
            LiveDataServer.ClosedConnection(CurrentSessionGuid, msg.CarId, msg.CarModel, msg.CarSkin, msg.DriverGuid, TrustToken);
        }

        public override void OnLapCompleted(MsgLapCompleted msg)
        {
            MsgCarInfo driver;
            if (!CarInfo.TryGetValue(msg.CarId, out driver))
                PluginManager.Log("Error; car_id " + msg.CarId + " was not known by the CarInfo Dictionary :(");
            else
            {
                PluginManager.Log("LapCompleted by " + driver.DriverName + ": " + TimeSpan.FromMilliseconds(msg.Laptime));
                var actions = LiveDataServer.LapCompleted(CurrentSessionGuid, msg.CarId, driver.DriverGuid, msg.Laptime, msg.Cuts, msg.GripLevel, ConvertLB(msg.Leaderboard), TrustToken);
                PluginManager.Log("" + actions.Length + " actions returned");
                HandleClientActions(actions);
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

                var actions = LiveDataServer.Collision(CurrentSessionGuid, msg.CarId, msg.OtherCarId, msg.RelativeVelocity, 0.667234f, msg.RelativePosition.x, msg.RelativePosition.z, msg.WorldPosition.x, msg.WorldPosition.z, TrustToken);
                HandleClientActions(actions);
            }
            else
            {
                PluginManager.Log("Collision occured!!! " + msg.CarId + " vs. wall");
                var result = LiveDataServer.Collision(CurrentSessionGuid, msg.CarId, -1, msg.RelativeVelocity, 0.667234f, msg.RelativePosition.x, msg.RelativePosition.z, msg.WorldPosition.x, msg.WorldPosition.z, TrustToken);
            }
        }

        private void EvaluateContactTree(CollisionBag bag)
        {
            var actions = LiveDataServer.CollisionTreeEnded(CurrentSessionGuid, bag.First, bag.Second, bag.Count, bag.Started, bag.LastCollision, TrustToken);
            HandleClientActions(actions);
            lock (contactTrees)
                contactTrees.Remove(bag);
        }

        private void HandleClientActions(PluginReaction[] actions)
        {
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
            if (msg.IsConnected)
                LiveDataServer.RandomCarInfo(CurrentSessionGuid, msg.CarId, msg.CarModel, msg.DriverName, msg.DriverGuid, TrustToken);
            else
                LiveDataServer.RandomCarInfo(CurrentSessionGuid, msg.CarId, msg.CarModel, "", "", TrustToken);
        }
    }
}
