using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace acPlugins4net.messages
{
    public class MsgClientLoaded : PluginMessage
    {
        public MsgClientLoaded()
            :base(kunos.ACSProtocol.MessageType.ACSP_CLIENT_LOADED)
        {

        }

        #region members as binary
        public byte CarId { get; private set; }
        #endregion

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
