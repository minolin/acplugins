using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net.messages
{
    public class RequestRealtimeInfo : PluginMessage
    {     
        public UInt16 Interval { get; set; }

        public RequestRealtimeInfo()
            : base(kunos.ACSProtocol.MessageType.ACSP_REALTIMEPOS_INTERVAL)
        {
        }

        protected internal override void Deserialize(BinaryReader br)
        {           
            Interval = br.ReadUInt16();
        }

        protected internal override void Serialize(BinaryWriter bw)
        {            
            bw.Write(Interval);
        }
    }
}
