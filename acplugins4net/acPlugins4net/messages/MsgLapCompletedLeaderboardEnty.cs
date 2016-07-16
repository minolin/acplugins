using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net.messages
{
    public class MsgLapCompletedLeaderboardEnty
    {
        public byte CarId { get; set; }
        public uint Laptime { get; set; }
        public ushort Laps { get; set; }
        public bool HasFinished { get; set; }

        public static MsgLapCompletedLeaderboardEnty FromBinaryReader(BinaryReader br)
        {
            return new MsgLapCompletedLeaderboardEnty()
            {
                CarId = br.ReadByte(),
                Laptime = br.ReadUInt32(),
                Laps = br.ReadUInt16(),
                HasFinished = br.ReadBoolean()
            };
        }

        internal void Serialize(BinaryWriter bw)
        {
            bw.Write(CarId);
            bw.Write(Laptime);
            bw.Write(Laps);
            bw.Write(HasFinished);
        }
    }
}
