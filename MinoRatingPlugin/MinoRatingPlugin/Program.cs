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
        private LiveDataDumpClient _LiveDataServer;
        public LiveDataDumpClient LiveDataServer
        {
            get
            {
                LastPluginActivity = DateTime.Now;
                return _LiveDataServer;
            }
            set
            {
                _LiveDataServer = value;
            }
        }
        public string TrustToken { get; set; }
        public Guid CurrentSessionGuid { get; set; }
        public DateTime CurrentSessionStartTime { get; set; }

        public DateTime LastPluginActivity { get; private set; }

        public static Version PluginVersion = new Version(2, 0, 0, 0);

        protected internal byte[] _fingerprint;

        private static LocalAuthCache _authCache = null;

        public TrackDefinition CurrentTrackDefinition { get; set; }

        #region Init code
        static void Main(string[] args)
        {
            AcServerPluginManager pluginManager = null;
            try
            {
                pluginManager = new AcServerPluginManager(new FileLogWriter("log", "minoplugin.txt") { CopyToConsole = true, LogWithTimestamp = true, }) { AcServerKeepAliveIntervalSeconds = 60 };

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
                catch (Exception) { }
            }
        }

        protected internal byte[] Hash(string s)
        {
            return new System.Security.Cryptography.MD5CryptoServiceProvider().ComputeHash(Encoding.Default.GetBytes(Environment.MachineName + s));
        }

        protected override void OnInit()
        {
            _fingerprint = Hash(PluginManager.Config.GetSetting("ac_server_directory") + PluginManager.RemotePort);

#if DEB1UG
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

            #region AUTH cache
            var authCachePort = System.Configuration.ConfigurationManager.AppSettings["local_auth_port"];
            if (!string.IsNullOrEmpty(authCachePort))
            {
                _authCache = new LocalAuthCache(int.Parse(authCachePort), PluginManager);
                _authCache.Run();
            }
            #endregion


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


            var server_config_ini = GatherServerConfigIni();

            CurrentSessionGuid = LiveDataServer.NewSessionWithConfig(CurrentSessionGuid, msg.ServerName, msg.Track + "[" + msg.TrackConfig + "]"
                , msg.SessionType, msg.Laps, msg.WaitTime, msg.SessionDuration, msg.AmbientTemp, msg.RoadTemp, msg.ElapsedMS
                , TrustToken, _fingerprint, PluginVersion, -1, -1, -1, server_config_ini);
            for (byte i = 0; i < 36; i++)
                PluginManager.RequestCarInfo(i);

            CurrentSessionStartTime = msg.CreationDate.AddMilliseconds(msg.ElapsedMS * -1);

            _distancesToReport.Clear();
            _consistencyReports.Clear();

            CurrentTrackDefinition = LiveDataServer.GetTrackDefinition(CurrentSessionGuid, msg.CreationDate);
        }

        private string GatherServerConfigIni()
        {
            try
            {
                var acDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                if (string.IsNullOrEmpty(acDirectory)) // happens in Debug mode
                    acDirectory = "";
                var configFile = System.IO.Path.Combine(acDirectory, 
                                                        System.Configuration.ConfigurationManager.AppSettings["ac_server_directory"],
                                                        System.Configuration.ConfigurationManager.AppSettings["ac_cfg_directory"],
                                                        "server_cfg.ini");

                var lines = System.IO.File.ReadAllLines(configFile);
                return string.Join(Environment.NewLine, lines.Where(x => !x.Contains("PASSWORD")));
            }
            catch (Exception ex)
            {
                return "EX: " + ex.Message;
            }
        }

        protected override void OnNewConnection(MsgNewConnection msg)
        {
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
            HandleClientActions(LiveDataServer.RandomCarInfo(CurrentSessionGuid, msg.CarId, "", "", "", false, GetCurrentRaceTimeMS(msg)));
        }

        protected override void OnLapCompleted(MsgLapCompleted msg)
        {
            DriverInfo driver;
            if (!PluginManager.TryGetDriverInfo(msg.CarId, out driver))
                PluginManager.Log("Error; car_id " + msg.CarId + " was not known by the PluginManager :(");
            else
            {
                TrySendDistance(driver, true);
                HandleClientActions(LiveDataServer.LapCompleted(CurrentSessionGuid, msg.CreationDate, msg.CarId, driver.DriverGuid, msg.Laptime, msg.Cuts, msg.GripLevel, ConvertLB(msg.Leaderboard)));
            }

            if (_consistencyReports.ContainsKey(driver) && _consistencyReports[driver] != null)
            {
                var cr = _consistencyReports[driver];
                _consistencyReports[driver] = null;
                cr.Cuts = msg.Cuts;
                cr.Laptime = msg.Laptime;
                HandleClientActions(LiveDataServer.LapCompletedConsistencySplits(CurrentSessionGuid, msg.CreationDate, msg.CarId, cr));
            }

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

        protected override void OnAcServerTimeout()
        {
            PluginManager.Log("OnAcServerTimeout()");
            LiveDataServer.EndSession(CurrentSessionGuid);
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
        }

        private void TrySendCollision(Guid currentSessionGuid, DateTime creationDate, int carId, int otherCarId, float relativeVelocity, float x1, float z1, float worldX, float worldZ, int bagId)
        {
            try
            {
                DriverInfo driver = null;
                if (!PluginManager.TryGetDriverInfo(Convert.ToByte(carId), out driver))
                    throw new Exception("Driver not found: " + carId);

                DriverInfo otherDriver = null;
                if (!PluginManager.TryGetDriverInfo(Convert.ToByte(otherCarId), out otherDriver))
                    throw new Exception("(Other) Driver not found: " + otherCarId);

                var driversCache = GetDriversCache(driver);
                var otherDriversCache = GetDriversCache(otherDriver);

                TrySendDistance(driver, true);
                HandleClientActions(LiveDataServer.CollisionV2(CurrentSessionGuid, creationDate, carId, otherCarId, relativeVelocity, driver.LastSplinePosition, x1, z1, worldX, worldZ, driversCache.ToArray(), otherDriversCache.ToArray(), bagId));
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
                TrySendDistance(driverInfo, true);
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

            if (!_consistencyReports.ContainsKey(di))
                _consistencyReports.Add(di, null);

            if (di.LastCarUpdate.List.Count > 1)
                if (TeleportedToPits(di))
                {
                    // This will invalidate the current lap,
                    di.CurrentLapStart = DateTime.Now;
                    _consistencyReports[di] = null;
                    // but we need to tell MR
                    HandleClientActions(LiveDataServer.DriverBackToPits(CurrentSessionGuid, di.LastCarUpdate.Value.CreationDate, di.CarId));
                }

            if (di.LastCarUpdate.List.Count > 1 && _consistencyReports[di] == null)
            {
                // We need to create a new CR. Important: The current speed is an indicator wether this is an inlap 
                var msgCarUpdate = di.LastCarUpdate.Value;
                _consistencyReports[di] = new ConsistencyReport() { carId = msgCarUpdate.CarId, LapStart = msgCarUpdate.CreationDate, MinGear = msgCarUpdate.Gear, MaxGear = msgCarUpdate.Gear, MinVelocity = di.CurrentSpeed, MaxVelocity = di.CurrentSpeed, SplitResolution = 10, Splits = new uint[0] };
            }

            if (di.LastCarUpdate.List.Count > 1)
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
                if (di.CurrentSpeed > cr.MaxVelocity)
                    cr.MaxVelocity = di.CurrentSpeed;

                float lastSplit = (int)(di.LastCarUpdate.Previous.Value.NormalizedSplinePosition * cr.SplitResolution);
                lastSplit /= cr.SplitResolution;
                float thisSplit = (int)(di.LastCarUpdate.Value.NormalizedSplinePosition * cr.SplitResolution);
                thisSplit /= cr.SplitResolution;

                if (thisSplit > lastSplit)
                {
                    // The SplinePos Split has been changed, so now we want to log the time.
                    // To be more precise we will try to recalculate the laptime in the exact transition
                    splits.Add(AverageLaptimeBySplit(cr.LapStart, di.LastCarUpdate.Previous.Value, di.LastCarUpdate.Value));
                }

                cr.Splits = splits.ToArray();
            }

            #endregion
        }

        private bool TeleportedToPits(DriverInfo di)
        {
            // Theory: if the car's velocity is zero in this and the last carUpdate, but there's some (significant) distance
            // between the world positions - it has to be a teleport to the pits (manually).
            // If only now is zero, the chances aren't bad that it was a penalty
            if (di.LastCarUpdate != null && di.LastCarUpdate.Previous != null)
            {
                var now = di.LastCarUpdate.Value;
                var last = di.LastCarUpdate.Previous.Value;

                if (Math.Round(now.Velocity.Length(), 0) == 0 && Math.Round(last.Velocity.Length()) == 0)
                {
                    var distance = (last.WorldPosition - now.WorldPosition).Length();
                    //PluginManager.Log("Car is standing; Distance = " + distance + " (" + (distance > 3) + ")");
                    if (distance > 3)
                        return true;
                }
            }

            return false;
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
                TrySendDistance(di);
            }
        }

        private void TrySendDistance(DriverInfo di, bool forced = false)
        {
            if (!_distancesToReport.ContainsKey(di))
                _distancesToReport.Add(di, new MRDistanceHelper());

            // New approach: We'll send the distance set as soon as a driver crosses a TrackDefinition.Split
            #region legacy approach: fixed distance
            if (CurrentTrackDefinition == null || CurrentTrackDefinition.Splits == null)
            {
                var distanceCached = _distancesToReport[di];
                // Then we'll do it in different resolutions; the first meters are more important than the later ones
                if (di.Distance > REGULAR_DISTANCE && distanceCached.MetersDriven > 2000 || forced) // After 2km, we'll just report in big chunks - or if forced
                {
                    HandleClientActions(LiveDataServer.DistanceDriven(CurrentSessionGuid, di.CarId, distanceCached));
                    _distancesToReport[di] = new MRDistanceHelper();
                }
                else if (di.Distance < REGULAR_DISTANCE && distanceCached.MetersDriven > 200) // 200m is about "left pits", so we'll report this until 
                {
                    HandleClientActions(LiveDataServer.DistanceDriven(CurrentSessionGuid, di.CarId, distanceCached));
                    _distancesToReport[di] = new MRDistanceHelper();
                }
            }
            #endregion
            else
            {
                try
                {
                    if (di.LastCarUpdate != null && di.LastCarUpdate.List.Count > 1)
                    {
                        var lastPos = di.LastCarUpdate.Previous.Value;
                        var thisPos = di.LastCarUpdate.Value;

                        foreach (var split in CurrentTrackDefinition.Splits)
                        {
                            if (lastPos.NormalizedSplinePosition < split && thisPos.NormalizedSplinePosition > split)
                            {
                                // Gotcha!
                                var distanceCached = _distancesToReport[di];
                                _distancesToReport[di] = new MRDistanceHelper();

                                distanceCached.SplinePosCurrent = thisPos.NormalizedSplinePosition;
                                distanceCached.SplinePosTimeCurrent = Convert.ToInt32(thisPos.CreationDate.Subtract(di.CurrentLapStart).TotalMilliseconds);
                                distanceCached.SplinePosLast = lastPos.NormalizedSplinePosition;
                                distanceCached.SplinePosTimeLast = Convert.ToInt32(lastPos.CreationDate.Subtract(di.CurrentLapStart).TotalMilliseconds);
                                HandleClientActions(LiveDataServer.DistanceDriven(CurrentSessionGuid, di.CarId, distanceCached));
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    PluginManager.Log(ex);
                }
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
                switch (a.Reaction)
                {
                    case PluginReaction.ReactionType.None:
                        break;
                    case PluginReaction.ReactionType.Whisper:
                        PluginManager.SendChatMessage(a.CarId, a.Text);
                        break;
                    case PluginReaction.ReactionType.Broadcast:
                        PluginManager.BroadcastChatMessage(a.Text);
                        break;
                    case PluginReaction.ReactionType.Ballast:
                        break;
                    case PluginReaction.ReactionType.Pit:
                        break;
                    case PluginReaction.ReactionType.Kick:
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
                        break;
                    case PluginReaction.ReactionType.Ban:
                        break;
                    case PluginReaction.ReactionType.NextSession:
                        PluginManager.NextSession();
                        break;
                    case PluginReaction.ReactionType.RestartSession:
                        PluginManager.RestartSession();
                        break;
                    case PluginReaction.ReactionType.AdminCmd:
                        PluginManager.AdminCommand(a.Text);
                        break;
                    default:
                        break;
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

        protected override void OnAcServerAlive()
        {
            HandleClientActions(_LiveDataServer.Alive(this.CurrentSessionGuid, DateTime.Now, null));
        }

        private void StartAliveTimer()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                while (true)
                {
                    // Sleeping for some seconds
                    Thread.Sleep(1000 * 90);

                    if (LastPluginActivity != DateTime.MinValue
                        && LastPluginActivity.AddSeconds(1000 * 90) < DateTime.Now)
                    {
                        var driversHash = "";
                        try
                        {
                            foreach (var d in PluginManager.GetDriverInfos().Where(x => x.IsConnected).OrderBy(x => x.CarId))
                            {
                                driversHash += d.DriverName;
                            }
                        }
                        catch (Exception ex)
                        {
                            PluginManager.Log(ex);
                        }
                        LiveDataServer.Alive(CurrentSessionGuid, DateTime.Now, "" + driversHash.GetHashCode());
                    }
                }
            });
        }

        #endregion
    }
}
