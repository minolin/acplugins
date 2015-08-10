using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace acPlugins4net.messages
{
    public class RequestSessionInfo : PluginMessage
    {
        /// <summary>
        /// ACSP_GET_SESSION_INFO gets a session index you want to get or a -1 for the current session 
        /// </summary>
        public Int16 SessionIndex { get; set; }

        public RequestSessionInfo()
            :base(kunos.ACSProtocol.MessageType.ACSP_GET_SESSION_INFO)
        {

        }

        protected internal override void Deserialize(BinaryReader br)
        {
            SessionIndex = br.ReadInt16();
        }

        protected internal override void Serialize(BinaryWriter bw)
        {
            bw.Write(SessionIndex);
        }
    }
}
