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
using acPlugins4net.info;

namespace MinoRatingPlugin
{
    public class MinoratingPlugin : AcServerPlugin
    {
        public LiveDataDumpClient LiveDataServer { get; set; }
        public string TrustToken { get; set; }
        public Guid CurrentSessionGuid { get; set; }
        public static Version PluginVersion = new Version(1, 2, 0);

        protected internal byte[] _fingerprint;


        #region Init code
        static void Main(string[] args)
        {
            try
            {
                AcServerPluginManager pluginManager = new AcServerPluginManager(new FileLogWriter("log", "minoplugin.txt") { CopyToConsole = true, LogWithTimestamp = true });
                pluginManager.LoadInfoFromServerConfig();
                pluginManager.AddPlugin(new MinoratingPlugin());
                pluginManager.LoadPluginsFromAppConfig();
                DriverInfo.MsgCarUpdateCacheSize = 10;
                pluginManager.RunUntilAborted();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        protected internal byte[] Hash(string s)
        {
            return new System.Security.Cryptography.MD5CryptoServiceProvider().ComputeHash(Encoding.Default.GetBytes(Environment.MachineName + s));
        }

        protected override void OnInit()
        {
            _fingerprint = Hash(PluginManager.Config.GetSetting("ac_server_directory") + PluginManager.RemotePort);

#if DEBUG
            LiveDataServer = new LiveDataDumpClient(new BasicHttpBinding(), new EndpointAddress("http://localhost:805/minorating/12"));
#else
            LiveDataServer = new LiveDataDumpClient(new BasicHttpBinding(), new EndpointAddress("http://plugin.minorating.com:805/minorating/12"));
#endif

            TrustToken = PluginManager.Config.GetSetting("server_trust_token");
            if (string.IsNullOrEmpty(TrustToken))
            {
                TrustToken = Guid.NewGuid().ToString();
                PluginManager.Config.SetSetting("server_trust_token", TrustToken);
            }
            CurrentSessionGuid = Guid.Empty;

            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    PluginManager.Log("Plugin Version " + PluginVersion);
                    var serverVersion = LiveDataServer.GetVersion();
                    PluginManager.Log("Connection to server with version: " + serverVersion);

                    if (serverVersion.Major > PluginVersion.Major)
                    {
                        PluginManager.Log("================================");
                        PluginManager.Log("================================");
                        PluginManager.Log("Version mismatch, minorating.com requires a newer version (" + serverVersion + " vs. " + PluginVersion + ")");
                        Environment.Exit(2);
                    }
                }
                catch (Exception ex)
                {
                    PluginManager.Log("Error connecting to the remote server :(");
                    PluginManager.Log(ex);
                    Environment.Exit(1);
                }
            });

            // Let's have a look if the acServer is already running
            try
            {
                PluginManager.RequestSessionInfo(-1);
            }
            catch (Exception)
            {
                Console.WriteLine("No acServer detected, waiting for a NewSession event");
            }
        }

        #endregion

        #region Simpler event overrides

        protected override void OnSessionInfo(MsgSessionInfo msg)
        {

            if (msg.Type == ACSProtocol.MessageType.ACSP_NEW_SESSION || CurrentSessionGuid == Guid.Empty)
                OnNewSession(msg);
        }

        protected override void OnNewSession(MsgSessionInfo msg)
        {
            PluginManager.Log("===============================");
            PluginManager.Log("===============================");
            PluginManager.Log("OnNewSession: " + msg.Name + "@" + msg.ServerName);
            PluginManager.Log("===============================");
            PluginManager.Log("===============================");

            CurrentSessionGuid = LiveDataServer.NewSession(CurrentSessionGuid, msg.ServerName, msg.Track + "[" + msg.TrackConfig + "]"
                , msg.SessionType, msg.Laps, msg.WaitTime, msg.SessionDuration, msg.AmbientTemp, msg.RoadTemp, msg.ElapsedMS
                , TrustToken, _fingerprint, PluginVersion, -1, -1, -1);
            for (byte i = 0; i < 36; i++)
                PluginManager.RequestCarInfo(i);

            _distancesToReport.Clear();
        }

        protected override void OnNewConnection(MsgNewConnection msg)
        {
            PluginManager.Log("OnNewConnection: " + msg.DriverName + "@" + msg.CarModel);
            HandleClientActions(LiveDataServer.RandomCarInfo(CurrentSessionGuid, msg.CarId, msg.CarModel, msg.DriverName, msg.DriverGuid, true));
        }

        protected override void OnSessionEnded(MsgSessionEnded msg)
        {
            PluginManager.Log("Session ended");
            HandleClientActions(LiveDataServer.EndSession(CurrentSessionGuid));
        }

        protected override void OnConnectionClosed(MsgConnectionClosed msg)
        {
            PluginManager.Log("OnConnectionClosed: " + msg.DriverName + "@" + msg.CarModel);
            HandleClientActions(LiveDataServer.RandomCarInfo(CurrentSessionGuid, msg.CarId, "", "", "", false));
        }

        protected override void OnLapCompleted(MsgLapCompleted msg)
        {
            PluginManager.Log(DateTime.Now.TimeOfDay.ToString() + "- OnLapCompleted: " + msg.CarId + ": " + TimeSpan.FromMilliseconds(msg.Laptime));
            DriverInfo driver;
            if (!PluginManager.TryGetDriverInfo(msg.CarId, out driver))
                PluginManager.Log("Error; car_id " + msg.CarId + " was not known by the PluginManager :(");
            else
            {
                SendDistance(driver, true);
                PluginManager.Log("LapCompleted by " + driver.DriverName + ": " + TimeSpan.FromMilliseconds(msg.Laptime));
                HandleClientActions(LiveDataServer.LapCompleted(CurrentSessionGuid, msg.CreationDate, msg.CarId, driver.DriverGuid, msg.Laptime, msg.Cuts, msg.GripLevel, ConvertLB(msg.Leaderboard)));
            }
        }


        protected override void OnCarInfo(MsgCarInfo msg)
        {
            PluginManager.Log(DateTime.Now.TimeOfDay.ToString() + "- CarInfo: " + msg.CarId + ", " + msg.DriverName + "@" + msg.CarModel + ", Connected=" + msg.IsConnected);

            // To prevent a bug in communication we will only send when the Car IsConnected - discos only via the corresponding event please.
            if (msg.IsConnected)
            {
                HandleClientActions(LiveDataServer.RandomCarInfo(CurrentSessionGuid, msg.CarId, msg.CarModel, msg.DriverName, msg.DriverGuid, msg.IsConnected));
            }

        }

        protected override void OnChatMessage(MsgChat msg)
        {
            if (!msg.IsCommand)
                return;

            var split = msg.Message.Split(' ');
            if (split.Length > 0)
            {
                switch (split[0].ToLower())
                {
                    case "/mr":
                    case "/minorating":
                        {
                            if (split.Length == 1) // only /mr 
                                HandleClientActions(LiveDataServer.RequestDriverRating(CurrentSessionGuid, msg.CarId));
                            else
                                HandleClientActions(LiveDataServer.RequestMRCommand(CurrentSessionGuid, msg.CarId, split));
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        protected override void OnClientLoaded(MsgClientLoaded msg)
        {
            HandleClientActions(LiveDataServer.RequestDriverLoaded(CurrentSessionGuid, msg.CarId));
        }

        #endregion

        #region Contact handling
        private List<CollisionBag> contactTrees = new List<CollisionBag>();

        protected override void OnCollision(MsgClientEvent msg)
        {
            if (msg.Subtype == (byte)ACSProtocol.MessageType.ACSP_CE_COLLISION_WITH_CAR)
            {
                // TODO: Messy code. Needs rewrite as soon as I know where I'm heading.
                // We'll check if the contact partners are part of an contact tree
                bool partOfATree = false;
                int bagId = -1;
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

                TrySendCollision(CurrentSessionGuid, msg.CreationDate, msg.CarId, msg.OtherCarId, msg.RelativeVelocity, msg.RelativePosition.X, msg.RelativePosition.Z, msg.WorldPosition.X, msg.WorldPosition.Z, bagId);
            }
            else
            {
                PluginManager.Log("Collision occured!!! " + msg.CarId + " vs. wall");
                TrySendCollision(CurrentSessionGuid, msg.CreationDate, msg.CarId, -1, msg.RelativeVelocity, msg.RelativePosition.X, msg.RelativePosition.Z, msg.WorldPosition.X, msg.WorldPosition.Z, -1);
            }
        }

        private void TrySendCollision(Guid currentSessionGuid, DateTime creationDate, int carId, int otherCarId, float relativeVelocity, float x1, float z1, float worldX, float worldZ, int bagId)
        {
            try
            {
                var driver = PluginManager.GetDriver(carId);
                List<CarUpdateHistory> driversCache = new List<CarUpdateHistory>();
                var carUpdate = driver.LastCarUpdate.Value;
                while (carUpdate != null && driversCache.Count < 6)
                {
                    driversCache.Add(new CarUpdateHistory()
                    {
                        Created = carUpdate.CreationDate,
                        NormalizedSplinePosition = carUpdate.NormalizedSplinePosition,
                        EngineRPM = carUpdate.EngineRPM,
                        Gear = carUpdate.Gear,
                        Velocity = new float[] { carUpdate.Velocity.X, carUpdate.Velocity.Z },
                        WorldPosition = new float[] { carUpdate.WorldPosition.X, carUpdate.WorldPosition.Z }
                    });
                }

                SendDistance(driver, true);
                HandleClientActions(LiveDataServer.Collision(CurrentSessionGuid, creationDate, carId, otherCarId, relativeVelocity, driver.LastSplinePosition, x1, z1, worldX, worldZ, driversCache.ToArray(), null, bagId));
            }
            catch (Exception ex)
            {
                PluginManager.Log(ex);
            }
        }

        private void EvaluateContactTree(CollisionBag bag)
        {
            lock (contactTrees)
                contactTrees.Remove(bag);

            var driverInfo = PluginManager.GetDriver(bag.First);
            if (driverInfo != null)
            {
                SendDistance(driverInfo, true);
                HandleClientActions(LiveDataServer.CollisionTreeEnded(CurrentSessionGuid, bag.First, bag.Second, bag.Count, bag.Started, bag.LastCollision));
            }

        }
        #endregion

        #region Distance driven & behaviour analysis

        private float TrackLength = 0.0f;
        private Dictionary<DriverInfo, MRDistanceHelper> _distancesToReport = new Dictionary<DriverInfo, MRDistanceHelper>();

        protected override void OnCarUpdate(DriverInfo di)
        {
            if (!_distancesToReport.ContainsKey(di))
                _distancesToReport.Add(di, new MRDistanceHelper());

            var dh = _distancesToReport[di];
            // Generally, the meters driven are stored
            dh.MetersDriven += di.LastDistanceTraveled;

            // To protect this from some simple 1st gear driving together in combat range to grind stuff, we'll only allow Attack & Combat range 
            // recording if there is acceleration. 3 or 5 are quite little values, even for slow cars like the GT86
            if (Math.Abs(di.CurrentAcceleration) > 2.0f && di.CurrentDistanceToClosestCar != 0)
            {
                // Then we'll check this interval (we're talking about a second or similar)
                // for driving in attack range (let's say.. inside 20m) or even combating (maybe 8m)
                if (di.CurrentDistanceToClosestCar < 8)
                    dh.MetersCombatRange += di.LastDistanceTraveled;
                else if (di.CurrentDistanceToClosestCar < 20)
                    dh.MetersAttackRange += di.LastDistanceTraveled;
            }
        }

        protected override void OnBulkCarUpdateFinished()
        {
            // Now we can report the distances driven to the minorating backend.
            foreach (var di in PluginManager.CurrentSession.Drivers)
            {
                // We won't report it per-secod or whatever interval is set, so we need to group by
                // sensible stuff - this needs to be tracked in the _reportedDistance
                SendDistance(di);
            }
        }

        private void SendDistance(DriverInfo di, bool forced = false)
        {
            if (!_distancesToReport.ContainsKey(di))
                _distancesToReport.Add(di, new MRDistanceHelper());

            var distanceCached = _distancesToReport[di];
            // Then we'll do it in different resolutions; the first meters are more important than the later ones
            if (di.Distance > REGULAR_DISTANCE && distanceCached.MetersDriven > 2000 || forced) // After 2km, we'll just report in big chunks - or if forced
            {
                PluginManager.Log(DateTime.Now.TimeOfDay.ToString() + "- Send DistanceDriven: " + di.CarId + ": " + distanceCached.MetersDriven);
                HandleClientActions(LiveDataServer.DistanceDriven(CurrentSessionGuid, di.CarId, distanceCached));
                _distancesToReport[di] = new MRDistanceHelper();
            }
            else if (di.Distance < REGULAR_DISTANCE && distanceCached.MetersDriven > 200) // 200m is about "left pits", so we'll report this until 
            {
                PluginManager.Log(DateTime.Now.TimeOfDay.ToString() + "- Send DistanceDriven: " + di.CarId + ": " + distanceCached.MetersDriven);
                HandleClientActions(LiveDataServer.DistanceDriven(CurrentSessionGuid, di.CarId, distanceCached));
                _distancesToReport[di] = new MRDistanceHelper();
            }
        }

        const int REGULAR_DISTANCE = 1000;

        #endregion

        #region Helpers & stuff
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
                // DEBUG TIME
                /*
                if(a.SteamId != "76561198021090310")
                {
                    if (string.IsNullOrEmpty(a.SteamId))
                        Console.WriteLine("No steam Id for action with text: " + a.Text);

                    return;
                }*/


                PluginManager.Log("Action for car " + a.CarId + ": " + a.Reaction + " " + a.Text);
                if (a.Reaction == PluginReaction.ReactionType.Whisper)
                    PluginManager.SendChatMessage(a.CarId, a.Text);
                else if (a.Reaction == PluginReaction.ReactionType.Broadcast)
                    PluginManager.BroadcastChatMessage(a.Text);
                else if (a.Reaction == PluginReaction.ReactionType.Kick)
                {
                    // To be 100% sure we kick the right person we'll have to compare the steam id
                    DriverInfo c;
                    if (this.PluginManager.TryGetDriverInfo(a.CarId, out c))
                        if (c.IsConnected && c.DriverGuid == a.SteamId)
                        {
                            PluginManager.BroadcastChatMessage("" + c.DriverName + " has been kicked by minorating.com");
                            PluginManager.RequestKickDriverById(a.CarId);
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Execute action: Error for car " + a.CarId + "/" + a.Text + ": " + ex.Message);
            }
        }

        // We have to convert the acPlugins4net-Leaderboard to a minoRating one. This is pretty stupid mapping
        LeaderboardEntry[] ConvertLB(List<MsgLapCompletedLeaderboardEnty> leaderboard)
        {
            var array = new LeaderboardEntry[leaderboard.Count];
            for (int i = 0; i < leaderboard.Count; i++)
            {
                string steamId;
                DriverInfo driver;
                if (this.PluginManager.TryGetDriverInfo(leaderboard[i].CarId, out driver))
                {
                    steamId = driver.DriverGuid;
                }
                else
                {
                    // should not happen
                    steamId = string.Empty;
                }

                array[i] = new LeaderboardEntry()
                {
                    CarId = leaderboard[i].CarId,
                    DriverId = steamId,
                    LapsDriven = leaderboard[i].Laps,
                    Time = leaderboard[i].Laptime
                };
            };

            return array;
        }

        #endregion
    }
}
