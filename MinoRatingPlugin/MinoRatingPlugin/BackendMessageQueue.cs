using System;
using MinoRatingPlugin.minoRatingServer;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using acPlugins4net;
using System.ServiceModel;
using System.Threading;
using acPlugins4net.info;

namespace MinoRatingPlugin
{
    public class BackendMessageQueue
    {
        private AcServerPluginManager PluginManager;
        private LiveDataDumpClient _mrserver;

        public string TrustToken { get; set; }
        public Guid CurrentSessionGuid { get; set; }
        public DateTime LastPluginActivity { get; private set; }
        public DateTime LastBackendActivity { get; private set; }

        #region Init stuff; only one time in the process lifecycle

        public BackendMessageQueue(AcServerPluginManager pluginManager)
        {
            PluginManager = pluginManager;
            Init();
        }

        private void Init()
        {
            _mrserver = new LiveDataDumpClient(new BasicHttpBinding(), new EndpointAddress("http://plugin.minorating.com:805/minorating/12"));
            TrustToken = PluginManager.Config.GetSetting("server_trust_token");
            if (string.IsNullOrEmpty(TrustToken))
            {
                TrustToken = Guid.NewGuid().ToString();
                PluginManager.Config.SetSetting("server_trust_token", TrustToken);
            }
            CurrentSessionGuid = Guid.Empty;

            CheckVersion();
        }

        void CheckVersion()
        {
            try
            {
                PluginManager.Log("Plugin Version " + MinoratingPlugin.PluginVersion);
                var serverVersion = _mrserver.GetVersion();
                PluginManager.Log("Connection to server with version: " + serverVersion);

                if (serverVersion.Major > MinoratingPlugin.PluginVersion.Major)
                {
                    PluginManager.Log("================================");
                    PluginManager.Log("================================");
                    PluginManager.Log("Version mismatch, minorating.com requires a newer version (" + serverVersion + " vs. " + MinoratingPlugin.PluginVersion + ")");
                    Environment.Exit(2);
                }
            }
            catch (Exception ex)
            {
                PluginManager.Log("Error connecting to the remote server :(");
                PluginManager.Log(ex);
                Environment.Exit(1);
            }
        }

        #endregion

        #region Synchronous events - bypasses any queueing for important reasons

        // Alive is important to send asynchronously; otherwise a living server could be closed
        // due to vast message queues
        public void SendAlive(string driversHash)
        {
            var pluginactions = _mrserver.Alive(CurrentSessionGuid, DateTime.Now, driversHash);
            HandleClientActions(pluginactions);
        }

        public TrackDefinition GetTrackDefinition(string currentTrack)
        {
            var trackDefinition = _mrserver.GetTrackDefinitionByName(currentTrack);
            LastBackendActivity = DateTime.Now;
            return trackDefinition;
        }

        public void CreateTrackLine(byte carId, float fromSpline, float toSpline, float x1, float y1, float x2, float y2, string hint, int type)
        {
            var reactions = _mrserver.CreateTrackLine(CurrentSessionGuid, carId, fromSpline, toSpline, x1, y1, x2, y2, hint, type);
            HandleClientActions(reactions);
        }

        #endregion

        #region Async events - will be queued and sent in the correct order. Return value should always be a collection of PluginActions

        public void NewSessionWithConfigAsync(string serverName, string track, byte sessionType, ushort laps, ushort waitTime, ushort sessionDuration, byte ambientTemp, byte roadTemp, int elapsedMS, string trustToken, byte[] fingerpint, Version pluginVersion, int sessionCollisionsToKick, int sessionMassAccidentsToKick, int serverKickMode, string server_Config_Ini)
        {
            CurrentSessionGuid = Guid.NewGuid();
            EnqueueBackendMessage(() =>
            {
                _mrserver.NewSessionWithConfig(CurrentSessionGuid, serverName, track, sessionType, laps, waitTime, sessionDuration, ambientTemp, roadTemp, elapsedMS, trustToken, fingerpint, pluginVersion, sessionCollisionsToKick, sessionMassAccidentsToKick, serverKickMode, server_Config_Ini);
                return new PluginReaction[0];
            });
        }

        public void EndSessionAsync()
        {
            _mrserver.EndSession(CurrentSessionGuid);
        }

        public void DriverBackToPitsAsync(byte carId, DateTime created)
        {
            EnqueueBackendMessage(() => { return _mrserver.DriverBackToPits(CurrentSessionGuid, created, carId); });
        }

        public void RandomCarInfoAsync(byte carId, string carModel, string driverName, string driverGuid, bool isConnected, int currentRaceTimeMS)
        {
            EnqueueBackendMessage(() => { return _mrserver.RandomCarInfo(CurrentSessionGuid, carId, carModel, driverName, driverGuid, isConnected, currentRaceTimeMS); });
        }

        public void LapCompletedAsync(DateTime creationDate, byte carId, string driverGuid, uint laptime, byte cuts, float gripLevel, LeaderboardEntry[] leaderboard)
        {
            EnqueueBackendMessage(() => { return _mrserver.LapCompleted(CurrentSessionGuid, creationDate, carId, driverGuid, laptime, cuts, gripLevel, leaderboard); });
        }

        public void RequestDriverRatingAsync(byte carId)
        {
            EnqueueBackendMessage(() => { return _mrserver.RequestDriverRating(CurrentSessionGuid, carId); });
        }

        public void RequestMRCommandAdminInfoAsync(byte carId, bool isAdmin, string[] str)
        {
            EnqueueBackendMessage(() => { return _mrserver.RequestMRCommandAdminInfo(CurrentSessionGuid, carId, isAdmin, str); });
        }

        public void RequestDriverLoadedAsync(byte carId)
        {
            EnqueueBackendMessage(() => { return _mrserver.RequestDriverLoaded(CurrentSessionGuid, carId); });
        }

        public void CollisionAsyncV22(DateTime creationDate, byte carId, byte otherCarId, float relativeVelocity, float lastSplinePosition, float relativeX, float relativeZ, float worldX, float worldZ, CarUpdateHistory[] historyCar, CarUpdateHistory[] historyOtherCar, MRDistanceHelper distanceCar)
        {
            EnqueueBackendMessage(() => { return _mrserver.CollisionV24(CurrentSessionGuid, creationDate, carId, otherCarId, relativeVelocity, lastSplinePosition, relativeX, relativeZ, worldX, worldZ, historyCar, historyOtherCar, distanceCar); });
        }

        public void LineCrossedAsync(byte carId, long lineId, float currentSpeed, float currentAcceleration, float minVelocity10s, float maxVelocity10s, float currentDistanceToClosestCar, float[] worldpositions)
        {
            EnqueueBackendMessage(() => { return _mrserver.LineCrossed(CurrentSessionGuid, carId, lineId, currentSpeed, currentAcceleration, minVelocity10s, maxVelocity10s, currentDistanceToClosestCar, worldpositions); });
        }

        public void DistanceDrivenAsync(byte carId, MRDistanceHelper distanceDriven)
        {
            EnqueueBackendMessage(() => { return _mrserver.DistanceDriven(CurrentSessionGuid, carId, distanceDriven); });
        }

        #endregion

        void EnqueueBackendMessage(Func<PluginReaction[]> action)
        {
            LastPluginActivity = DateTime.Now;
            // Quick&Dirty: Direct execution

            ThreadPool.QueueUserWorkItem(o =>
            {
                var reaction = action();
                HandleClientActions(reaction);
            });
        }

        #region Handle Plugin Actions

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

        private static object _executeLock = new object();

        private void ExecuteAction(PluginReaction a)
        {
            if (a == null)
                return;

            LastBackendActivity = DateTime.Now;

            lock (_executeLock)
            {
                try
                {
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
        }

        #endregion
    }
}
