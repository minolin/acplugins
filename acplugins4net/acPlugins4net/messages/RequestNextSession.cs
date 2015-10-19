using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net.messages
{
    public class RequestNextSession : PluginMessage
    {
        public RequestNextSession()
        : base(kunos.ACSProtocol.MessageType.ACSP_NEXT_SESSION)
        {

        }

        protected internal override void Deserialize(BinaryReader br)
        {

        }

        protected internal override void Serialize(BinaryWriter bw)
        {

        }
    }
}
