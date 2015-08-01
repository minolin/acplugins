using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using acPlugins4net.kunos;
namespace acPlugins4net.messages
{
    public class MsgSessionEnded : PluginMessage
    {
        #region As-binary-members; we should reuse them exactly this way to stay efficient
        public string ReportFileName { get; set; }
        #endregion        

        public MsgSessionEnded()
            : base(ACSProtocol.MessageType.ACSP_END_SESSION)
        {
        }

        protected internal override void Serialize(System.IO.BinaryWriter bw)
        {
            writeStringW(bw, ReportFileName);
        }

        protected internal override void Deserialize(System.IO.BinaryReader br)
        {
            ReportFileName = readStringW(br);
        }
    }
}
