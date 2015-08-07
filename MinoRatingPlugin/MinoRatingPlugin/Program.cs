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

namespace MinoRatingPlugin
{
    class Program : AcServerPlugin
    {
        public LiveDataDumpClient LiveDataServer { get; set; }
        public string TrustToken { get; set; }
        public Guid CurrentSessionGuid { get; set; }
        private bool _sessionIdPending = false;
        public static Version PluginVersion = new Version(0, 3, 0);

        private void WaitForCleanSessionId()
        {
            while (_sessionIdPending)
                Thread.Sleep(10);
        }

        static void Main(string[] args)
        {
            System.IO.File.WriteAllText("minoratingplugin.txt", "Main(): " + DateTime.Now);
            new Program() { PluginName = "Minorating " + PluginVersion }.RunUntilAborted();
        }

        public override void OnInit()
        {
            base.OnInit();
            LiveDataServer = new LiveDataDumpClient();
            TrustToken = Config.GetSetting("server_trust_token");
            if (string.IsNullOrEmpty(TrustToken))
            {
                TrustToken = Guid.NewGuid().ToString();
                Config.SetSetting("server_trust_token", TrustToken);
            }
            CurrentSessionGuid = Guid.Empty;

            new Thread(() =>
            {
                try
                {
                    Console.WriteLine("Plugin Version " + PluginVersion);
                    var serverVersion = LiveDataServer.GetVersion();
                    Console.WriteLine("Connection to server with version: " + serverVersion);

                    if (serverVersion > PluginVersion)
                    {
                        Console.WriteLine("================================");
                        Console.WriteLine("================================");
                        Console.WriteLine("Version missmatch, your plugin seems to be outdated. Please consider downloading a new one from the forums");
                        Console.WriteLine("For the moment we'll do our best and try to go on.");
                        Console.WriteLine("================================");
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Error connecting to the remote server :(");
                }
            }).Start();


        }

        public override void OnNewSession(MsgNewSession msg)
        {
            _sessionIdPending = true;
            Console.WriteLine("===============================");
            Console.WriteLine("===============================");
            Console.WriteLine("OnNewSession: " + msg.Name + "@" + Servername);
            Console.WriteLine("===============================");
            Console.WriteLine("===============================");
            CurrentSessionGuid = LiveDataServer.NewSession(CurrentSessionGuid, Servername, Track + "[" + TrackLayout + "]", msg.SessionType, msg.Laps, msg.WaitTime, msg.TimeOfDay, msg.AmbientTemp, msg.RoadTemp, TrustToken, _fingerprint);
            _sessionIdPending = false;

            BroadcastChatMessage("This server is observed by www.minorating.com - stay clean & have fun");
        }

        public override void OnNewConnection(MsgNewConnection msg)
        {
            WaitForCleanSessionId();
            Console.WriteLine("OnNewConnection: " + msg.DriverName + "@" + msg.CarModel);

            LiveDataServer.NewConnection(CurrentSessionGuid, msg.CarId, msg.CarModel, msg.DriverName, msg.DriverGuid, TrustToken);
        }

        public override void OnSessionEnded(MsgSessionEnded msg)
        {
            LiveDataServer.EndSession(CurrentSessionGuid, TrustToken, _fingerprint);
            Console.WriteLine("Session ended");
        }

        public override void OnConnectionClosed(MsgConnectionClosed msg)
        {
            WaitForCleanSessionId();
            Console.WriteLine("OnConnectionClosed: " + msg.DriverName + "@" + msg.CarModel);
            LiveDataServer.ClosedConnection(CurrentSessionGuid, msg.CarId, msg.CarModel, msg.CarSkin, msg.DriverGuid, TrustToken);
        }

        public override void OnLapCompleted(MsgLapCompleted msg)
        {
            WaitForCleanSessionId();
            MsgCarInfo driver;
            if (!CarInfo.TryGetValue(msg.CarId, out driver))
                Console.WriteLine("Error; car_id " + msg.CarId + " was not known by the CarInfo Dictionary :(");
            else
            {
                Console.WriteLine("LapCompleted by " + driver.DriverName + ": " + TimeSpan.FromMilliseconds(msg.Laptime));
                var result = LiveDataServer.LapCompleted(CurrentSessionGuid, msg.CarId, driver.DriverGuid, msg.Laptime, msg.Cuts, msg.GripLevel, ConvertLB(msg.Leaderboard), TrustToken);
            }
        }

        private List<CollisionBag> contactTrees = new List<CollisionBag>();

        public override void OnCollision(MsgClientEvent msg)
        {
            WaitForCleanSessionId();
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

                    if (!partOfATree)
                    {
                        // Then we'll start a new one
                        contactTrees.Add(CollisionBag.StartNew(msg.CarId, msg.OtherCarId, EvaluateContactTree));
                    }
                }

                Console.WriteLine("Collision occured!!! " + msg.CarId + " vs. " + msg.OtherCarId);
                var result = LiveDataServer.Collision(CurrentSessionGuid, msg.CarId, msg.OtherCarId, msg.RelativeVelocity, 0.667234f, msg.RelativePosition.x, msg.RelativePosition.z, msg.WorldPosition.x, msg.WorldPosition.z, TrustToken);
            }
            else
            {
                Console.WriteLine("Collision occured!!! " + msg.CarId + " vs. wall");
                var result = LiveDataServer.Collision(CurrentSessionGuid, msg.CarId, -1, msg.RelativeVelocity, 0.667234f, msg.RelativePosition.x, msg.RelativePosition.z, msg.WorldPosition.x, msg.WorldPosition.z, TrustToken);
            }
        }

        private void EvaluateContactTree(CollisionBag bag)
        {
            lock (contactTrees)
                contactTrees.Remove(bag);

            var actions = LiveDataServer.CollisionTreeEnded(CurrentSessionGuid, bag.First, bag.Second, bag.Count, bag.Started, bag.LastCollision, TrustToken);
            foreach (var a in actions)
            {
                // TODO switch (a.Reaction)
                Console.WriteLine("Action for car " + a.CarId + ": " + a.Reaction + " " + a.Text);

                if(!string.IsNullOrEmpty(a.Text))
                if(a.Reaction == CollisionReaction.ReactionType.Whisper)
                    SendChatMessage(a.CarId, a.Text);
                else if (a.Reaction == CollisionReaction.ReactionType.Broadcast)
                    BroadcastChatMessage(a.Text);
            }
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
            WaitForCleanSessionId();
            Console.WriteLine("CarInfo: " + msg.CarId + ", " + msg.DriverName + "@" + msg.CarModel);
            if (msg.IsConnected)
                LiveDataServer.RandomCarInfo(CurrentSessionGuid, msg.CarId, msg.CarModel, msg.DriverName, msg.DriverGuid, TrustToken);
            else
                LiveDataServer.RandomCarInfo(CurrentSessionGuid, msg.CarId, msg.CarModel, "", "", TrustToken);
        }
    }
}
