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
using System.Windows;

namespace MinoRatingPlugin
{
    public class MinoratingPlugin : AcServerPlugin
    {
        public BackendMessageQueue MRBackend { get; set; }

        public string TrustToken { get; set; }
        public DateTime CurrentSessionStartTime { get; set; }

        public static Version PluginVersion = new Version(2, 4, 2, 0);

        protected internal byte[] _fingerprint;

        private static LocalAuthCache _authCache = null;

        public TrackDefinition CurrentTrackDefinition { get; set; }
        public Rect? PitExitRectangle { get; set; }

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
                DriverInfo.MsgCarUpdateCacheSize = 100;

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

        protected override bool OnCommandEntered(string cmd)
        {
            if (cmd == "status")
            {
                Console.WriteLine("Plugin status:");
                Console.WriteLine($"\tCurrentSession:\t{MRBackend.CurrentSessionGuid}");
                Console.WriteLine($"\tCurrentTrack:\t{CurrentTrackDefinition?.TrackName}");
                Console.WriteLine($"\tLastMsgSent:\t{(DateTime.Now - MRBackend.LastPluginActivity).TotalSeconds:F3}s");
                Console.WriteLine($"\tLastMsgReceived:\t{(DateTime.Now - PluginManager.LastServerActivity).TotalSeconds:F3}s");
            }
            else
                return false;

            return true;
        }

        protected override void OnInit()
        {
            _fingerprint = Hash(PluginManager.Config.GetSetting("ac_server_directory") + PluginManager.RemotePort);

            MRBackend = new BackendMessageQueue(PluginManager);


            #region AUTH cache

            var authCachePort = System.Configuration.ConfigurationManager.AppSettings["local_auth_port"];
            if (!string.IsNullOrEmpty(authCachePort))
            {
                _authCache = new LocalAuthCache(int.Parse(authCachePort), PluginManager);
                _authCache.Run();
            }

            #endregion

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
            if (msg.Type == ACSProtocol.MessageType.ACSP_NEW_SESSION || MRBackend.CurrentSessionGuid == Guid.Empty)
                OnNewSession(msg);
        }

        protected override void OnNewSession(MsgSessionInfo msg)
        {
            PluginManager.EnableRealtimeReport(100);
            PluginManager.Log("===============================");
            PluginManager.Log("===============================");
            PluginManager.Log("OnNewSession: " + msg.Name + "@" + msg.ServerName);
            PluginManager.Log("===============================");
            PluginManager.Log("===============================");

            var server_config_ini = GatherServerConfigIni();
            var trackId = msg.Track + "[" + msg.TrackConfig + "]";

            MRBackend.NewSessionWithConfigAsync(msg.ServerName, trackId
                , msg.SessionType, msg.Laps, msg.WaitTime, msg.SessionDuration, msg.AmbientTemp, msg.RoadTemp, msg.ElapsedMS
                , TrustToken, _fingerprint, PluginVersion, -1, -1, -1, server_config_ini);

            var maxDrivers = TryParseMaxDrivers(server_config_ini);
            PluginManager.Log($"Max drivers: {maxDrivers}");

            for (byte i = 0; i < maxDrivers; i++)
                PluginManager.RequestCarInfo(i);

            CurrentSessionStartTime = msg.CreationDate.AddMilliseconds(msg.ElapsedMS * -1);

            _distancesToReport.Clear();

            if (trackId != CurrentTrackDefinition?.TrackName)
                ReloadTrackDefinition(trackId);
        }

        private void ReloadTrackDefinition(string trackId = null)
        {
            if (trackId == null)
                trackId = CurrentTrackDefinition?.TrackName;

            if (trackId != null)
            {
                CurrentTrackDefinition = MRBackend.GetTrackDefinition(trackId);
                CreatePitExitRectangle();
                PluginManager.Log($"Pit exit rectangle created: {PitExitRectangle}");
                PluginManager.Log($"Track Lines parsed: {CurrentTrackDefinition?.Lines?.Length ?? 0}");
            }
        }

        protected override void OnNewConnection(MsgNewConnection msg)
        {
            MRBackend.RandomCarInfoAsync(msg.CarId, msg.CarModel, msg.DriverName, msg.DriverGuid, true, GetCurrentRaceTimeMS(msg));
        }

        protected override void OnSessionEnded(MsgSessionEnded msg)
        {
            PluginManager.Log("Session ended");
            MRBackend.EndSessionAsync();
        }

        protected override void OnConnectionClosed(MsgConnectionClosed msg)
        {
            MRBackend.RandomCarInfoAsync(msg.CarId, "", "", "", false, GetCurrentRaceTimeMS(msg));
        }

        protected override void OnLapCompleted(MsgLapCompleted msg)
        {
            DriverInfo driver;
            if (!PluginManager.TryGetDriverInfo(msg.CarId, out driver))
                PluginManager.Log("Error; car_id " + msg.CarId + " was not known by the PluginManager :(");
            else
            {
                driver.IsOnOutlap = false;
                TrySendDistance(driver, true);
                MRBackend.LapCompletedAsync(msg.CreationDate, msg.CarId, driver.DriverGuid, msg.Laptime, msg.Cuts, msg.GripLevel, ConvertLB(msg.Leaderboard));
            }
        }

        protected override void OnCarInfo(MsgCarInfo msg)
        {
            PluginManager.Log(DateTime.Now.TimeOfDay.ToString() + "- CarInfo: " + msg.CarId + ", " + msg.DriverName + "@" + msg.CarModel + ", Connected=" + msg.IsConnected);

            // To prevent a bug in communication we will only send when the Car IsConnected - discos only via the corresponding event please.
            if (msg.IsConnected)
            {
                MRBackend.RandomCarInfoAsync(msg.CarId, msg.CarModel, msg.DriverName, msg.DriverGuid, msg.IsConnected, GetCurrentRaceTimeMS(msg));
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
                                MRBackend.RequestDriverRatingAsync(msg.CarId);
                            else
                                MRBackend.RequestMRCommandAdminInfoAsync(msg.CarId, PluginManager.GetDriverInfo(msg.CarId).IsAdmin, split);
                        }
                        break;
                    case "/mrpoint":
                        {
                            string text;
                            DriverInfo driver = null;
                            if (!PluginManager.TryGetDriverInfo(Convert.ToByte(msg.CarId), out driver))
                                text = "Driver not found: " + msg.CarId;
                            else if (driver?.LastCarUpdate?.Value == null)
                                text = "Something's wrong";
                            else
                            {
                                var upd = driver?.LastCarUpdate?.Value;

                                text = $"Spl:{upd.NormalizedSplinePosition:F5}|X={upd.WorldPosition.X:F5}|Z={upd.WorldPosition.Z:F5}";
                            }

                            PluginManager.SendChatMessage(msg.CarId, text);
                        }
                        break;
                    case "/mrinfo":
                        {
                            PluginManager.SendChatMessage(msg.CarId, $"Track id: {CurrentTrackDefinition?.TrackName}, length ={CurrentTrackDefinition?.Length:N0}");
                            PluginManager.SendChatMessage(msg.CarId, $"Pit exit: {PitExitRectangle?.X}");
                        }
                        break;
                    case "/mrtrackreload":
                        {
                            CurrentTrackDefinition = MRBackend.GetTrackDefinition(CurrentTrackDefinition.TrackName);
                            CreatePitExitRectangle();
                            PluginManager.SendChatMessage(msg.CarId, $"Track reloaded");
                        }
                        break;
                    case "/mrtl1":
                        {
                            string text;
                            DriverInfo driver = null;
                            if (!PluginManager.TryGetDriverInfo(Convert.ToByte(msg.CarId), out driver))
                                text = "Driver not found: " + msg.CarId;
                            else if (driver?.LastCarUpdate?.Value == null)
                                text = "Something's wrong";
                            else
                            {
                                _AdminAddTrackLineStart = driver?.LastCarUpdate?.Value;
                                text = $"Start set: {_AdminAddTrackLineStart}";
                            }

                            PluginManager.SendChatMessage(msg.CarId, text);
                        }
                        break;
                    case "/mrtl2":
                        {
                            Console.WriteLine("TrackLine 2");
                            string text = null;
                            int type = 2; // pit exit = 1, default is line = 2
                            DriverInfo driver = null;
                            if (_AdminAddTrackLineStart == null)
                                text = "No start set";
                            else if (!PluginManager.TryGetDriverInfo(Convert.ToByte(msg.CarId), out driver))
                                text = "Driver not found: " + msg.CarId;
                            else if (driver?.LastCarUpdate?.Value == null)
                                text = "Something's wrong";
                            else if (split.Length < 2 || string.IsNullOrEmpty(split[1]))
                                text = "No hint set";
                            else
                            {
                                if (split.Length == 3)
                                    type = int.Parse(split[2]);

                                try
                                {
                                    var startPoint = _AdminAddTrackLineStart;
                                    var endPoint = driver?.LastCarUpdate?.Value;
                                    MRBackend.CreateTrackLine(msg.CarId,
                                                              startPoint.NormalizedSplinePosition,
                                                              endPoint.NormalizedSplinePosition,
                                                              startPoint.WorldPosition.X,
                                                              startPoint.WorldPosition.Z,
                                                              endPoint.WorldPosition.X,
                                                              endPoint.WorldPosition.Z,
                                                              split[1],
                                                              type);
                                    Console.WriteLine($"Message sent, type {type}");
                                    ReloadTrackDefinition();
                                    PluginManager.SendChatMessage(msg.CarId, "Track reloaded");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Exception in TrackLine2: {ex.ToString()}");
                                }
                            }

                            if (!string.IsNullOrEmpty(text))
                                PluginManager.SendChatMessage(msg.CarId, text);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Little helper for an admin command that expresses Minolin's laziness. Do not use it
        /// </summary>
        MsgCarUpdate _AdminAddTrackLineStart = null;

        protected override void OnClientLoaded(MsgClientLoaded msg)
        {
            MRBackend.RequestDriverLoadedAsync(msg.CarId);
        }

        protected override void OnAcServerTimeout()
        {
            PluginManager.Log("OnAcServerTimeout()");
            MRBackend.EndSessionAsync();
        }

        #endregion

        #region Contact handling

        private List<CollisionBag> contactTrees = new List<CollisionBag>();

        protected override void OnCollision(MsgClientEvent msg)
        {
            // Contact handling is now done by the backend completely - that is we just report any 
            // collision with another car
            if (msg.Subtype == (byte)ACSProtocol.MessageType.ACSP_CE_COLLISION_WITH_CAR)
            {
                DriverInfo driver = null;
                if (!PluginManager.TryGetDriverInfo(Convert.ToByte(msg.CarId), out driver))
                    throw new Exception("Driver not found: " + msg.CarId);

                DriverInfo otherDriver = null;
                if (!PluginManager.TryGetDriverInfo(Convert.ToByte(msg.OtherCarId), out otherDriver))
                    throw new Exception("(Other) Driver not found: " + msg.OtherCarId);

                var driversCache = GetDriversCache(driver, 980);
                var otherDriversCache = GetDriversCache(otherDriver, 980);

                var driversDistance = _distancesToReport[driver];
                _distancesToReport[driver] = new MRDistanceHelper();

                TrySendDistance(driver, true);
                MRBackend.CollisionAsyncV22(msg.CreationDate, msg.CarId, msg.OtherCarId, msg.RelativeVelocity, driver.LastSplinePosition, msg.RelativePosition.X, msg.RelativePosition.Z, msg.WorldPosition.X, msg.WorldPosition.Z, driversCache.ToArray(), otherDriversCache.ToArray(), driversDistance);
            }
        }

        private List<CarUpdateHistory> GetDriversCache(DriverInfo driver, int maxInterval)
        {
            List<CarUpdateHistory> driversCache = new List<CarUpdateHistory>();
            var node = driver.LastCarUpdate;
            while (node != null && node.Value != null)
            {
                var carUpdate = node.Value;
                var lastInserted = driversCache.LastOrDefault();

                if (lastInserted == null
                    || Math.Abs((lastInserted.Created - carUpdate.CreationDate).TotalMilliseconds) > maxInterval)
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

                node = node.Previous;
            }
            return driversCache;
        }

        #endregion

        #region Distance driven & behaviour analysis

        private Dictionary<DriverInfo, MRDistanceHelper> _distancesToReport = new Dictionary<DriverInfo, MRDistanceHelper>();

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

            #region Outlap detection

            if (!di.IsOnOutlap && PitExitRectangle.HasValue)
            {
                di.IsOnOutlap = PitExitRectangle.Value.Contains(new System.Windows.Point(di.LastPosition.X, di.LastPosition.Z));
                if (di.IsOnOutlap)
                {
                    MRBackend.DriverBackToPitsAsync(di.CarId, DateTime.Now);
                }
            }

            #endregion

            #region teleported to pits detection

            if (TeleportedToPits(di))
                // Special trick: If we assume a teleport, we set the outlap to false
                // so the outlap detection above can trigger again
                di.IsOnOutlap = false;

            #endregion

            #region Line crossing

            try
            {
                if (CurrentTrackDefinition?.Lines != null && di.LastCarUpdate?.Previous != null)
                    // For performance we'll just calc the lines that are between the corresponding SplinePosition frame
                    foreach (var l in CurrentTrackDefinition.Lines.Where(x => x.FromSpline <= di.LastSplinePosition && x.ToSpline >= di.LastSplinePosition))
                    {
                        var isIntersecting = IsIntersecting(l, di.LastCarUpdate);
                        //PluginManager.BroadcastChatMessage($"{l.LineId}: {isIntersecting}");

                        // We are between the focus zone for this (l)ine. Did we cross it?
                        if (isIntersecting)
                        {
                            // That's worth a message to the backend then!
                            // Just need the min and max velocity of the latest entries
                            var driversCache = GetDriversCache(di, 98);
                            var driversVelocities = driversCache.Select(x => new Vector3f(x.Velocity[0], 0, x.Velocity[1]).Length() * 3.6f);

                            var worldPositions = new List<float>();
                            foreach (var item in driversCache.Select(x => x.WorldPosition))
                                worldPositions.AddRange(item);

                            var distanceToNextCar = di.CurrentDistanceToClosestCar;
                            if (distanceToNextCar == 0) // = no other cars around
                                distanceToNextCar = 99999;
                            MRBackend.LineCrossedAsync(di.CarId, l.LineId, di.CurrentSpeed, di.CurrentAcceleration, driversVelocities.Min(), driversVelocities.Max(), distanceToNextCar, worldPositions.ToArray());
                        }
                    }
            }
            catch (Exception ex)
            {
                PluginManager.Log(ex);
            }

            #endregion
        }

        /// <summary>
        /// Calculates if and where the given line was crossed 
        /// </summary>
        /// <param name="trackDefinitionLine"></param>
        /// <param name="lastCarUpdate"></param>
        /// <returns></returns>
        bool IsIntersecting(TrackDefinitionLine trackDefinitionLine, LinkedListNode<MsgCarUpdate> lastCarUpdate)
        {
            var lastPos = lastCarUpdate.Value.WorldPosition;
            var prevPos = lastCarUpdate.Previous.Value.WorldPosition;

            return IsIntersecting(new Point(trackDefinitionLine.FromX, trackDefinitionLine.FromZ),
                                  new Point(trackDefinitionLine.ToX, trackDefinitionLine.ToZ),
                                  new Point(prevPos.X, prevPos.Z),
                                  new Point(lastPos.X, lastPos.Z));
        }

        bool IsIntersecting(Point a, Point b, Point c, Point d)
        {
            float denominator = ((b.X - a.X) * (d.Y - c.Y)) - ((b.Y - a.Y) * (d.X - c.X));
            float numerator1 = ((a.Y - c.Y) * (d.X - c.X)) - ((a.X - c.X) * (d.Y - c.Y));
            float numerator2 = ((a.Y - c.Y) * (b.X - a.X)) - ((a.X - c.X) * (b.Y - a.Y));

            // Detect coincident lines (has a problem, read below)
            if (denominator == 0) return numerator1 == 0 && numerator2 == 0;

            float r = numerator1 / denominator;
            float s = numerator2 / denominator;

            return (r >= 0 && r <= 1) && (s >= 0 && s <= 1);
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
                    MRBackend.DistanceDrivenAsync(di.CarId, distanceCached);
                    _distancesToReport[di] = new MRDistanceHelper();
                }
                else if (di.Distance < REGULAR_DISTANCE && distanceCached.MetersDriven > 200) // 200m is about "left pits", so we'll report this until 
                {
                    MRBackend.DistanceDrivenAsync(di.CarId, distanceCached);
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
                                MRBackend.DistanceDrivenAsync(di.CarId, distanceCached);
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

        int TryParseMaxDrivers(string server_Config_Ini)
        {
            var line = server_Config_Ini.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(x => x.StartsWith("MAX_CLIENTS="));
            if (string.IsNullOrEmpty(line))
                return 36;

            var value = line.Replace("MAX_CLIENTS=", "").Trim();

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return 36;
            }
        }

        private void CreatePitExitRectangle()
        {
            if (CurrentTrackDefinition?.PitExitRectangle == null || CurrentTrackDefinition.PitExitRectangle.Count() != 4)
            {
                PitExitRectangle = null;
            }
            else
            {
                var per = CurrentTrackDefinition.PitExitRectangle;
                PitExitRectangle = new Rect(Math.Min(per[0], per[2]),
                                            Math.Min(per[1], per[3]),
                                            Math.Abs(per[0] - per[2]),
                                            Math.Abs(per[1] - per[3]));
            }
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
                    Time = leaderboard[i].Laptime,
                    HasFinished = leaderboard[i].HasFinished,
                };
            };

            return array;
        }

        private string GetConnectedDriversHash()
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

            return "" + driversHash.GetHashCode();
        }

        private int GetCurrentRaceTimeMS(PluginMessage msg)
        {
            if (CurrentSessionStartTime == DateTime.MinValue)
                return 0;
            return (int)Math.Round((msg.CreationDate - CurrentSessionStartTime).TotalMilliseconds);
        }

        protected override void OnAcServerAlive()
        {
            MRBackend.SendAlive(GetConnectedDriversHash());
        }

        #endregion
    }
}
