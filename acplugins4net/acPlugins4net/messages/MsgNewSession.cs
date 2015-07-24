using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using acPlugins4net.kunos;
namespace acPlugins4net.messages
{
    public class MsgNewSession : PluginMessage
    {
        #region As-binary-members; we should reuse them exactly this way to stay efficient
        public string Name { get; set; }
        public byte SessionType { get; set; }
        public ushort TimeOfDay { get; set; }
        public ushort Laps { get; set; }
        public ushort WaitTime { get; set; }
        public byte AmbientTemp { get; set; }
        public byte RoadTemp { get; set; }
        public string Weather { get; set; }
        #endregion

        #region wellformed stuff members - offer some more comfortable data conversion

        public TimeSpan TimeOfDayTimespan
        {
            get
            {
                return TimeSpan.FromMinutes(TimeOfDay);
            }

            set
            {
                TimeOfDay = Convert.ToUInt16(Math.Round(value.TotalMinutes, 0));
            }
        }

        [Obsolete("Not obsolete, but the values are just guesses. Need to check that")]
        public enum SessionTypeEnum : byte { Practise = 0, Qualifying = 1, Race = 2, Drag = 3, Drift = 4 }

        #endregion

        public MsgNewSession(ACSProtocol.MessageType type)
            : base(type)
        {

        }


        public override string StringRepresentation
        {
            get
            {
                return
                    "Name=" + Name + NL +
                    "Type=" + SessionType + NL +
                    "Time=" + TimeOfDay + NL +
                    "Laps=" + Laps + NL +
                    "WaitTime=" + WaitTime + NL +
                    "Ambient=" + AmbientTemp + NL +
                    "Road=" + RoadTemp + NL +
                    "Weather=" + Weather;
            }
        }

        protected internal override void Serialize(System.IO.BinaryWriter bw)
        {
            writeString(bw, Name);
            bw.Write(SessionType);
            bw.Write(TimeOfDay);
            bw.Write(Laps);
            bw.Write(WaitTime);
            bw.Write(AmbientTemp);
            bw.Write(RoadTemp);
            writeString(bw, Weather);
        }

        protected internal override void Deserialize(System.IO.BinaryReader br)
        {
            Name = readString(br);
            SessionType = br.ReadByte();
            TimeOfDay = br.ReadUInt16();
            Laps = br.ReadUInt16();
            WaitTime = br.ReadUInt16();
            AmbientTemp = br.ReadByte();
            RoadTemp = br.ReadByte();
            Weather = readString(br);
        }
    }
}
