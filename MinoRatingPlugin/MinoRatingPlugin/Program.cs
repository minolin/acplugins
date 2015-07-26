using acPlugins4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using acPlugins4net.messages;
using MinoRatingPlugin.minoRatingServer;

namespace MinoRatingPlugin
{
    class Program : AcServerPlugin
    {
        public LiveDataDumpClient LiveDataServer { get; set; }
        public string TrustToken { get; set; }
        public Guid CurrentSessionGuid { get; set; }

        static void Main(string[] args)
        {
            new Program().RunUntilAborted();
        }

        public override void OnInit()
        {
            base.OnInit();
            LiveDataServer = new LiveDataDumpClient();
            TrustToken = Config.GetSetting("server_trust_token");
            CurrentSessionGuid = Guid.Empty;
        }

        public override void OnNewSession(MsgNewSession msg)
        {
            Console.WriteLine("New Connection: " + msg.Name);
            CurrentSessionGuid = LiveDataServer.NewSession(CurrentSessionGuid, msg.Name, Track, msg.SessionType, msg.Laps, msg.WaitTime, msg.TimeOfDay, msg.AmbientTemp, msg.RoadTemp, TrustToken);
        }

        public override void OnLapCompleted(MsgLapCompleted msg)
        {
            var driver = CarInfo[msg.CarId];
            var result = LiveDataServer.LapCompleted(CurrentSessionGuid, msg.CarId, driver.DriverGuid, msg.Laptime, msg.Cuts, msg.GripLevel, ConvertLB(msg.Leaderboard), TrustToken);
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

    }
}
