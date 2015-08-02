using acPlugins4net.messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acServerFake.viemodel.messages
{
    public class LapCompletedViewModel : BaseMessageViewModel<MsgLapCompleted>
    {
        public override string MsgCaption
        {
            get
            {
                return "Lap completed";
            }
        }

        public LapCompletedViewModel()
        {
            Message.CarId = 7;
            Message.Cuts = 0;
            Message.GripLevel = 0.99f;
            Message.Laptime = 92652;
            Message.Leaderboard = new List<MsgLapCompletedLeaderboardEnty>();
            Message.Leaderboard.AddRange(new MsgLapCompletedLeaderboardEnty[]
            {
                new MsgLapCompletedLeaderboardEnty() { CarId = 7, Laptime = Message.Laptime, Laps = 4 },
                new MsgLapCompletedLeaderboardEnty() { CarId = 3, Laptime = 930353, Laps = 2 },
                new MsgLapCompletedLeaderboardEnty() { CarId = 0, Laptime = 119652, Laps = 1 }
            });
        }
    }
}
