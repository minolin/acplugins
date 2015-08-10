using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using acPlugins4net.kunos;
namespace acPlugins4net.messages
{
    public class MsgSessionInfo : PluginMessage
    {
        /// <summary>
        /// Not sure about Drag and Drift, but those probably don't exist in multiplayer.
        /// </summary>
        public enum SessionTypeEnum : byte { Practice = 1, Qualifying = 2, Race = 3, Drag = 4, Drift = 5 }

        #region As-binary-members; we should reuse them exactly this way to stay efficient
        public byte Version { get; set; }
        public string ServerName { get; set; }
        public string TrackConfig { get; set; }
        public string Track { get; set; }
        public string Name { get; set; }
        public byte SessionType { get; set; }
        public ushort TimeOfDay { get; set; }
        public ushort Laps { get; set; }
        public ushort WaitTime { get; set; }
        public byte AmbientTemp { get; set; }
        public byte RoadTemp { get; set; }
        public string Weather { get; set; }
        /// <summary>
        /// Milliseconds from the start (this might be negative for races with WaitTime)
        /// </summary>
        public int ElapsedMS { get; private set; }
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
        /// The index of the session in the message
        /// </summary>
        public byte SessionIndex { get; private set; }
        /// <summary>
        /// The index of the current session in the server
        /// </summary>
        public byte CurrentSessionIndex { get; private set; }
        /// <summary>
        /// The number of sessions in the server
        /// </summary>
        public byte SessionCount { get; private set; }


        #endregion

        public MsgSessionInfo()
            : base(ACSProtocol.MessageType.ACSP_SESSION_INFO)
        {

        }

        protected internal override void Deserialize(System.IO.BinaryReader br)
        {
            Version = br.ReadByte();
            SessionIndex = br.ReadByte(); 
            CurrentSessionIndex = br.ReadByte();
            SessionCount = br.ReadByte();
            ServerName = readStringW(br);
            Track = readString(br);
            TrackConfig = readString(br);
            Name = readString(br);
            SessionType = br.ReadByte();
            TimeOfDay = br.ReadUInt16();
            Laps = br.ReadUInt16();
            WaitTime = br.ReadUInt16();
            AmbientTemp = br.ReadByte();
            RoadTemp = br.ReadByte();
            Weather = readString(br);
            ElapsedMS = br.ReadInt32();
        }

        protected internal override void Serialize(System.IO.BinaryWriter bw)
        {
            bw.Write(Version);
            bw.Write(SessionIndex);
            bw.Write(CurrentSessionIndex);
            bw.Write(SessionCount);
            bw.Write(ServerName);
            bw.Write(Track);
            bw.Write(TrackConfig);
            writeString(bw, Name);
            bw.Write(SessionType);
            bw.Write(TimeOfDay);
            bw.Write(Laps);
            bw.Write(WaitTime);
            bw.Write(AmbientTemp);
            bw.Write(RoadTemp);
            writeString(bw, Weather);
            bw.Write(ElapsedMS);
        }
    }
}
