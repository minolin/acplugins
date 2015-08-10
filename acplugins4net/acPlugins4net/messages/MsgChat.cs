using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace acPlugins4net.messages
{
    public class MsgChat : PluginMessage
    {
        public MsgChat()
            :base(kunos.ACSProtocol.MessageType.ACSP_CHAT)
        {

        }

        #region members as binary
        public byte CarId { get; private set; }
        public string Message { get; private set; }
        #endregion

        public bool IsCommand
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Message))
                    return false;
                return Message.StartsWith("/");
            }
        }

        protected internal override void Deserialize(BinaryReader br)
        {
            CarId = br.ReadByte();
            Message = readStringW(br);
        }

        protected internal override void Serialize(BinaryWriter bw)
        {
            bw.Write(CarId);
            bw.Write(Message);
        }
    }
}
