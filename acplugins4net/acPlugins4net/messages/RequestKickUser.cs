using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace acPlugins4net.messages
{
    public class RequestKickUser : PluginMessage
    {
        public byte CarId { get; set; }

        public RequestKickUser()
            :base(kunos.ACSProtocol.MessageType.ACSP_KICK_USER)
        {

        }

        protected internal override void Deserialize(BinaryReader br)
        {
            CarId = br.ReadByte();
        }

        protected internal override void Serialize(BinaryWriter bw)
        {
            bw.Write(CarId);
        }
    }
}
