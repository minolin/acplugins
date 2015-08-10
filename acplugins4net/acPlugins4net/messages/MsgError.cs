using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace acPlugins4net.messages
{
    public class MsgError: PluginMessage
    {
        public string ErrorMessage { get; private set; }

        public MsgError()
            :base(kunos.ACSProtocol.MessageType.ACSP_ERROR)
        {

        }

        protected internal override void Deserialize(BinaryReader br)
        {
            ErrorMessage = readStringW(br);
        }

        protected internal override void Serialize(BinaryWriter bw)
        {
            bw.Write(ErrorMessage);
        }
    }
}
