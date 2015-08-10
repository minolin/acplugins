using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace acPlugins4net.messages
{
    public class RequestSetSession : PluginMessage
    {
        public RequestSetSession()
            :base(kunos.ACSProtocol.MessageType.ACSP_SET_SESSION_INFO)
        {

        }

        public byte SessionIndex { get; set; }
        public string ServerName { get; set; }
        public byte SessionType { get; set; }
        public UInt32 Laps { get; set; }
        /// <summary>
        /// Time (of day?) in seconds
        /// </summary>
        public UInt32 Time { get; set; }
        /// <summary>
        /// Wait time (before race/lock pits in qualifying) in seconds
        /// </summary>
        public UInt32 WaitTime { get; set; }

        protected internal override void Deserialize(BinaryReader br)
        {
            SessionIndex = br.ReadByte();
            ServerName = readStringW(br);
            SessionType = br.ReadByte();
            Laps = br.ReadUInt32();
            Time = br.ReadUInt32();
            WaitTime = br.ReadUInt32();
        }

        protected internal override void Serialize(BinaryWriter bw)
        {
            // Session Index we want to change, be very careful with changing the current session tho, some stuff might not work as expected
            bw.Write(SessionIndex);

            // Session name
            writeStringW(bw, ServerName); // Careful here, the server is still broadcasting ASCII strings to the clients for this

            // Session type
            bw.Write(SessionType);

            // Laps
            bw.Write(Laps);

            // Time (in seconds)
            bw.Write(Time);

            // Wait time (in seconds)
            bw.Write(WaitTime);

        }
    }
}
