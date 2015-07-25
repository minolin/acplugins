using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net.messages
{
    public class RequestSendChat : PluginMessage
    {
        public byte CarId { get; set; }
        public string ChatMessage { get; set; }

        public RequestSendChat()
            :base(kunos.ACSProtocol.MessageType.ACSP_SEND_CHAT)
        {

        }

        protected internal override void Deserialize(BinaryReader br)
        {
            CarId = br.ReadByte();
            ChatMessage = readStringW(br);
        }

        protected internal override void Serialize(BinaryWriter bw)
        {
            bw.Write(CarId);
            writeStringW(bw, ChatMessage);
        }
    }
}
