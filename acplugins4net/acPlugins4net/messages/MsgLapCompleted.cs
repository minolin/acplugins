using System.Collections.Generic;
using System.IO;

namespace acPlugins4net.messages
{
    public class MsgLapCompleted : PluginMessage
    {
        public byte CarId { get; set; }
        public uint Laptime { get; set; }
        public byte Cuts { get; set; }
        public byte LeaderboardSize { get { return (byte)Leaderboard.Count; } }
        public List<MsgLapCompletedLeaderboardEnty> Leaderboard { get; set; }
        public float GripLevel { get; set; }

        public MsgLapCompleted()
            : base(kunos.ACSProtocol.MessageType.ACSP_LAP_COMPLETED)
        {
            Leaderboard = new List<MsgLapCompletedLeaderboardEnty>();
        }

        protected internal override void Deserialize(BinaryReader br)
        {
            CarId = br.ReadByte();
            Laptime = br.ReadUInt32();
            Cuts = br.ReadByte();

            var leaderboardCount = br.ReadByte();
            Leaderboard.Clear();
            for (int i = 0; i < leaderboardCount; i++)
            {
                Leaderboard.Add(MsgLapCompletedLeaderboardEnty.FromBinaryReader(br));
            }

            GripLevel = br.ReadSingle();
        }

        protected internal override void Serialize(BinaryWriter bw)
        {
            bw.Write(CarId);
            bw.Write(Laptime);
            bw.Write(Cuts);
            bw.Write(LeaderboardSize);

            foreach (var entry in Leaderboard)
            {
                entry.Serialize(bw);
            }

            bw.Write(GripLevel);
        }
    }
}
