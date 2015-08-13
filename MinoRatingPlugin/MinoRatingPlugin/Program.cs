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
        public static Version PluginVersion = new Version(0, 4, 0);

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
                RunPluginInConsole.RunUntilAborted(pluginManager);
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
            LiveDataServer = new LiveDataDumpClient(new BasicHttpBinding(), new EndpointAddress("http://localhost:806/minorating"));
#else
            LiveDataServer = new LiveDataDumpClient(new BasicHttpBinding(), new EndpointAddress("http://plugin.minorating.com:806/minorating"));
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
            CurrentSessionGuid = LiveDataServer.NewSession(CurrentSessionGuid, msg.ServerName, msg.Track + "[" + msg.TrackConfig + "]", msg.SessionType, msg.Laps, msg.WaitTime, msg.SessionDuration, msg.AmbientTemp, msg.RoadTemp, TrustToken, _fingerprint, PluginVersion);
        }

        protected override void OnNewConnection(MsgNewConnection msg)
        {
            PluginManager.Log("OnNewConnection: " + msg.DriverName + "@" + msg.CarModel);
            HandleClientActions(LiveDataServer.ClientConnected(CurrentSessionGuid, msg.CarId, msg.CarModel, msg.DriverName, msg.DriverGuid));
        }

        protected override void OnSessionEnded(MsgSessionEnded msg)
        {
            PluginManager.Log("Session ended");
            HandleClientActions(LiveDataServer.EndSession(CurrentSessionGuid));
        }

        protected override void OnConnectionClosed(MsgConnectionClosed msg)
        {
            PluginManager.Log("OnConnectionClosed: " + msg.DriverName + "@" + msg.CarModel);
            HandleClientActions(LiveDataServer.ClientDisconnected(CurrentSessionGuid, msg.CarId, msg.CarModel, msg.CarSkin, msg.DriverGuid));
        }

        protected override void OnLapCompleted(MsgLapCompleted msg)
        {
            DriverInfo driver;
            if (!PluginManager.TryGetDriverInfo(msg.CarId, out driver))
                PluginManager.Log("Error; car_id " + msg.CarId + " was not known by the PluginManager :(");
            else
            {
                PluginManager.Log("LapCompleted by " + driver.DriverName + ": " + TimeSpan.FromMilliseconds(msg.Laptime));
                HandleClientActions(LiveDataServer.LapCompleted(CurrentSessionGuid, msg.CarId, driver.DriverGuid, msg.Laptime, msg.Cuts, msg.GripLevel, ConvertLB(msg.Leaderboard)));
            }
        }


        protected override void OnCarInfo(MsgCarInfo msg)
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

        protected override void OnChatMessage(MsgChat msg)
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

        protected override void OnClientLoaded(MsgClientLoaded msg)
        {
            HandleClientActions(LiveDataServer.RequestDriverRating(CurrentSessionGuid, msg.CarId));
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

                HandleClientActions(LiveDataServer.Collision(CurrentSessionGuid, msg.CarId, msg.OtherCarId, msg.RelativeVelocity, 0.667234f, msg.RelativePosition.X, msg.RelativePosition.Z, msg.WorldPosition.X, msg.WorldPosition.Z));
            }
            else
            {
                PluginManager.Log("Collision occured!!! " + msg.CarId + " vs. wall");
                HandleClientActions(LiveDataServer.Collision(CurrentSessionGuid, msg.CarId, -1, msg.RelativeVelocity, 0.667234f, msg.RelativePosition.X, msg.RelativePosition.Z, msg.WorldPosition.X, msg.WorldPosition.Z));
            }
        }

        private void EvaluateContactTree(CollisionBag bag)
        {
            lock (contactTrees)
                contactTrees.Remove(bag);
            HandleClientActions(LiveDataServer.CollisionTreeEnded(CurrentSessionGuid, bag.First, bag.Second, bag.Count, bag.Started, bag.LastCollision));
        }
        #endregion

        #region Distance driven & behaviour analysis

        private float TrackLength = 0.0f;

        protected override void OnBulkCarUpdateFinished()
        {
            if (TrackLength == 0f && false) // A fucking mess this is. Tried cool and then simpler things; I won't get it working right :( See https://github.com/minolin/acplugins/issues/24
            {
                // As long as we have no SplinePos->Meters factor, well just put every single (world-position/splinepos) pair we
                // can get into a big list and try to calc it. This will happen once per server start (= possible track change) only
                var list = new List<SplinePosCalculationHelper>();
                PluginManager.CurrentSession.Drivers.ForEach((driverInfo) =>
                {
                    var node = driverInfo.LastCarUpdate;
                    while (node != null)
                    {
                        list.Add(new SplinePosCalculationHelper(node.Value));
                        node = node.Previous;
                    }
                });

                TryCalcSplinePos(list);
            }

            if (TrackLength > 0)
            {
                // as soon as there is a TrackLength we can easily estimate the gap between cars by multiplying the spline pos 
                // with the track length. Tomorrow.

            }
        }

        private void TryCalcSplinePos(List<SplinePosCalculationHelper> list)
        {
            // We try to shape the SplinePos-factor by calculating all the world positions
            // until we have a very marginally error left
            // Edit: No, we won't. Wth the math fucked me today (although I'm not sure if math or double precision with huge numbers)
            double totalTrackLengths = 0;
            int totalCount = 0;

            for (int i = 0; i < list.Count && list.Count > 1; i++)
                for (int j = i + 1; j < list.Count; j++)
                    if (list[i].CalcDifference(list[j], ref totalTrackLengths))
                        totalCount++;

            // we will ignore this, if not enough points have been regarded. Let's say a single car should do the calculations
            // if on the track, then the calculation should kick with the cache size. Tricky stuff, if the Cache is too small we should wait for 1 or 2 other cars.
            // 5 points on the track would at give 15 results
            if (totalCount > 15)
            {
                TrackLength = (float)(totalTrackLengths / totalCount);
                PluginManager.BroadcastChatMessage(string.Format("The track is {0:N0}m / {1:F1} = {2:N0} long", totalTrackLengths, totalCount, TrackLength));
            }
        }

        internal class SplinePosCalculationHelper
        {
            Vector3f WorldPos;
            float SplinePos;

            internal SplinePosCalculationHelper(MsgCarUpdate msg)
            {
                WorldPos = msg.WorldPosition;
                SplinePos = msg.NormalizedSplinePosition;
            }

            internal bool CalcDifference(SplinePosCalculationHelper other, ref double resultSum)
            {
                var m = Math.Abs((other.WorldPos - this.WorldPos).Length());
                var s = Math.Abs(other.SplinePos - this.SplinePos);
                while (s < 0)
                    s++;
                while (s > 1)
                    s--;

                if (m < 22.2f // We will be much better with higher values. The pit limit is 22.2 m/s, after that we will have a pretty good estimate 
                    || s > 0.5) // The finish line is nasty and could lead to 0.99 difference - ignore that 
                    return false;

                // I tried to sum both meters and spline-diff for a more precise result - didn't happen. No idea why.
                resultSum += (m / s);
                return true;
            }
        }

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
            catch (Exception) { }
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
