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
        public DateTime CurrentSessionStartTime { get; set; }
        public static Version PluginVersion = new Version(1, 3, 1, 0);

        protected internal byte[] _fingerprint;


        #region Init code
        static void Main(string[] args)
        {
            AcServerPluginManager pluginManager = null;
            try
            {
                pluginManager = new AcServerPluginManager(new FileLogWriter("log", "minoplugin.txt") { CopyToConsole = true, LogWithTimestamp = true });
                pluginManager.LoadInfoFromServerConfig();
                pluginManager.AddPlugin(new MinoratingPlugin());
                pluginManager.LoadPluginsFromAppConfig();
                DriverInfo.MsgCarUpdateCacheSize = 10;
                pluginManager.RunUntilAborted();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                try
                {
                    pluginManager.Log(ex);
                }
                catch (Exception){}
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
            LiveDataServer = new LiveDataDumpClient(new BasicHttpBinding(), new EndpointAddress("http://localhost:805/minorating"));
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

            CurrentSessionStartTime = msg.CreationDate.AddMilliseconds(msg.ElapsedMS*-1);

            _distancesToReport.Clear();
        }

        protected override void OnNewConnection(MsgNewConnection msg)
        {
            PluginManager.Log("OnNewConnection: " + msg.DriverName + "@" + msg.CarModel);
            HandleClientActions(LiveDataServer.RandomCarInfo(CurrentSessionGuid, msg.CarId, msg.CarModel, msg.DriverName, msg.DriverGuid, true, GetCurrentRaceTimeMS(msg)));
        }

        private int GetCurrentRaceTimeMS(PluginMessage msg)
        {
            if (CurrentSessionStartTime == DateTime.MinValue)
                return 0;
            return (int)Math.Round((msg.CreationDate - CurrentSessionStartTime).TotalMilliseconds);
        }

        protected override void OnSessionEnded(MsgSessionEnded msg)
        {
            PluginManager.Log("Session ended");
            HandleClientActions(LiveDataServer.EndSession(CurrentSessionGuid));
        }

        protected override void OnConnectionClosed(MsgConnectionClosed msg)
        {
            PluginManager.Log("OnConnectionClosed: " + msg.DriverName + "@" + msg.CarModel);
            HandleClientActions(LiveDataServer.RandomCarInfo(CurrentSessionGuid, msg.CarId, "", "", "", false, GetCurrentRaceTimeMS(msg)));
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

            if (_consistencyReports.ContainsKey(driver) && _consistencyReports[driver] != null)
            {
                PluginManager.Log("CR for driver available, will send");
                _consistencyReports[driver].Cuts = msg.Cuts;
                _consistencyReports[driver].Laptime = msg.Laptime;
                _consistencyReports[driver].MaxVelocity = driver.TopSpeed;
                HandleClientActions(LiveDataServer.LapCompletedConsistencySplits(CurrentSessionGuid, msg.CreationDate, msg.CarId, _consistencyReports[driver]));
            }

            _consistencyReports[driver] = new ConsistencyReport() { carId = msg.CarId, LapStart = msg.CreationDate, MinGear = 8, MinVelocity = 400, SplitResolution = 10, Splits = new uint[0] };
        }


        protected override void OnCarInfo(MsgCarInfo msg)
        {
            PluginManager.Log(DateTime.Now.TimeOfDay.ToString() + "- CarInfo: " + msg.CarId + ", " + msg.DriverName + "@" + msg.CarModel + ", Connected=" + msg.IsConnected);

            // To prevent a bug in communication we will only send when the Car IsConnected - discos only via the corresponding event please.
            if (msg.IsConnected)
            {
                HandleClientActions(LiveDataServer.RandomCarInfo(CurrentSessionGuid, msg.CarId, msg.CarModel, msg.DriverName, msg.DriverGuid, msg.IsConnected, GetCurrentRaceTimeMS(msg)));
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
                                HandleClientActions(LiveDataServer.RequestMRCommandAdminInfo(CurrentSessionGuid, msg.CarId, PluginManager.GetDriverInfo(msg.CarId).IsAdmin, split));
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
                            bagId = ct.BagId;
                            break;
                        }
                    }
                    PluginManager.Log("" + DateTime.Now.TimeOfDay + " OnCollision (" + msg.CarId + "vs" + msg.OtherCarId + "), contantTrees.Count=" + contactTrees.Count + ", partOfATree=" + partOfATree);

                    if (!partOfATree)
                    {
                        // Then we'll start a new one
                        var newBag = CollisionBag.StartNew(msg.CarId, msg.OtherCarId, EvaluateContactTree, PluginManager);
                        contactTrees.Add(newBag);
                        bagId = newBag.BagId;
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
                DriverInfo driver = null;
                if(!PluginManager.TryGetDriverInfo(Convert.ToByte(carId), out driver))
                    throw new Exception("Driver not found: " + carId);

                DriverInfo otherDriver = null;
                if (!PluginManager.TryGetDriverInfo(Convert.ToByte(otherCarId), out otherDriver))
                    throw new Exception("(Other) Driver not found: " + otherCarId);

                var driversCache = GetDriversCache(driver);
                var otherDriversCache = GetDriversCache(otherDriver);

                SendDistance(driver, true);
                HandleClientActions(LiveDataServer.CollisionV2(CurrentSessionGuid, creationDate, carId, otherCarId, relativeVelocity, driver.LastSplinePosition, x1, z1, worldX, worldZ, driversCache.ToArray(), otherDriversCache.ToArray(), bagId));
                PluginManager.Log("Did send this");
            }
            catch (Exception ex)
            {
                PluginManager.Log(ex);
            }
        }

        private List<CarUpdateHistory> GetDriversCache(DriverInfo driver)
        {
            List<CarUpdateHistory> driversCache = new List<CarUpdateHistory>();
            var node = driver.LastCarUpdate;
            while (node != null && node.Value != null && driversCache.Count < 6)
            {
                var carUpdate = node.Value;
                driversCache.Add(new CarUpdateHistory()
                {
                    Created = carUpdate.CreationDate,
                    NormalizedSplinePosition = carUpdate.NormalizedSplinePosition,
                    EngineRPM = carUpdate.EngineRPM,
                    Gear = carUpdate.Gear,
                    Velocity = new float[] { carUpdate.Velocity.X, carUpdate.Velocity.Z },
                    WorldPosition = new float[] { carUpdate.WorldPosition.X, carUpdate.WorldPosition.Z }
                });

                node = node.Previous;
            }
            return driversCache;
        }

        private void EvaluateContactTree(CollisionBag bag)
        {
            lock (contactTrees)
                contactTrees.Remove(bag);

            DriverInfo driverInfo = null;
            if (!PluginManager.TryGetDriverInfo(Convert.ToByte(bag.First), out driverInfo))
                return;

            if (driverInfo != null)
            {
                SendDistance(driverInfo, true);
                HandleClientActions(LiveDataServer.CollisionTreeEndedV2(CurrentSessionGuid, bag.First, bag.Second, bag.Count, bag.Started, bag.LastCollision));
            }

        }
        #endregion

        #region Distance driven & behaviour analysis

        private Dictionary<DriverInfo, MRDistanceHelper> _distancesToReport = new Dictionary<DriverInfo, MRDistanceHelper>();
        private Dictionary<DriverInfo, ConsistencyReport> _consistencyReports = new Dictionary<DriverInfo, ConsistencyReport>();

        protected override void OnCarUpdate(DriverInfo di)
        {
            #region Distance
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
            #endregion

            #region Consistency

            if (_consistencyReports.ContainsKey(di) && di.LastCarUpdate.List.Count > 1)
            {
                var cr = _consistencyReports[di];
                var splits = cr.Splits.ToList();
                var carUpdate = di.LastCarUpdate.Value;

                if (carUpdate.Gear > 1 && cr.MinGear > carUpdate.Gear)
                    cr.MinGear = carUpdate.Gear;
                if (cr.MaxGear < carUpdate.Gear)
                    cr.MaxGear = carUpdate.Gear;
                if (di.CurrentSpeed < cr.MinVelocity)
                    cr.MinVelocity = di.CurrentSpeed;
                if (di.CurrentSpeed < cr.MinVelocity)
                    cr.MinVelocity = di.CurrentSpeed;

                float lastSplit = (int)(di.LastCarUpdate.Previous.Value.NormalizedSplinePosition * cr.SplitResolution);
                lastSplit /= cr.SplitResolution;
                float thisSplit = (int)(di.LastCarUpdate.Value.NormalizedSplinePosition * cr.SplitResolution);
                thisSplit /= cr.SplitResolution;

                if (lastSplit == 0.9f && thisSplit == 0f)
                    Console.WriteLine("NewLap detected for " + di.DriverName + ". SplitCount: " + cr.Splits.Length);
                else if (lastSplit > thisSplit)
                { 
                    Console.WriteLine("Aborted lap detected for " + di.DriverName);
                    _consistencyReports.Remove(di);
                }
                if (lastSplit != thisSplit)
                {
                    Console.WriteLine(string.Format("LastSplit={0:f2}, ThisSplit={1:f2}, splines={2:f2}/{3:f2}", lastSplit, thisSplit, di.LastCarUpdate.Previous.Value.NormalizedSplinePosition, di.LastCarUpdate.Value.NormalizedSplinePosition));
                    // The SplinePos Split has been changed, so now we want to log the time.
                    // To be more precise we will try to recalculate the laptime in the exact transition
                    splits.Add(AverageLaptimeBySplit(cr.LapStart, di.LastCarUpdate.Previous.Value, di.LastCarUpdate.Value));
                    Console.WriteLine("Added: " + splits.Last() + " (Count=" + splits.Count + ")");
                }

                cr.Splits = splits.ToArray();
            }

            #endregion
        }

        public uint AverageLaptimeBySplit(DateTime lapStart, MsgCarUpdate tminus1, MsgCarUpdate t0)
        {
            // We need to remove the failure that the 1s interval brings. Basically we want to know
            // (or calculate) when the driver exactly was at the spline-split


            // Difference of the Splines
            var splineDiff = t0.NormalizedSplinePosition - tminus1.NormalizedSplinePosition;
            // Split
            float splineSplit = (int)(t0.NormalizedSplinePosition * 10);
            splineSplit /= 10;
            
            // Ratio: 
            var ratio = (t0.NormalizedSplinePosition - splineSplit) / splineDiff;

            // Timediff in MS
            var timeDiff = 1000 - t0.CreationDate.Subtract(tminus1.CreationDate).TotalMilliseconds;
            var timeOff = timeDiff * (1 - ratio);

            var result = (uint)Math.Round(t0.CreationDate.Subtract(lapStart).TotalMilliseconds - timeOff);
            string.Format("Spline={0:F3}, TimeOff={1:N0}ms, Result={2:N0}", t0.NormalizedSplinePosition, timeOff, result);

            return result;
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
        private void HandleClientActions(PluginReactionCollection actions)
        {
            if (actions == null)
                throw new ArgumentNullException("PluginReactionCollection actions", "Looks like the server didn't create an empty PluginReaction array");

            HandleClientActions(actions.Reactions);
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
