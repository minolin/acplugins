using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net.messages
{
    public class RequestBroadcastChat : PluginMessage
    {
        public string ChatMessage { get; set; }

        public RequestBroadcastChat()
        : base(kunos.ACSProtocol.MessageType.ACSP_BROADCAST_CHAT)
        {

        }

        protected internal override void Deserialize(BinaryReader br)
        {
            ChatMessage = readStringW(br);
        }

        protected internal override void Serialize(BinaryWriter bw)
        {
            writeStringW(bw, ChatMessage);
        }
    }
}
