using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net.messages
{
    public class RequestAdminCommand : PluginMessage
    {
        public string Command { get; set; }

        public RequestAdminCommand()
        : base(kunos.ACSProtocol.MessageType.ACSP_ADMIN_COMMAND)
        {

        }

        protected internal override void Deserialize(BinaryReader br)
        {
            Command = readStringW(br);
        }

        protected internal override void Serialize(BinaryWriter bw)
        {
            writeStringW(bw, Command);
        }
    }
}
