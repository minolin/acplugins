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

        /// <summary>
        /// Not sure about Drag and Drift, but those probably don't exist in multiplayer.
        /// </summary>
        public enum SessionTypeEnum : byte { Practice = 1, Qualifying = 2, Race = 3, Drag = 4, Drift = 5 }

        #endregion

        public MsgNewSession()
            : base(ACSProtocol.MessageType.ACSP_NEW_SESSION)
        {

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
