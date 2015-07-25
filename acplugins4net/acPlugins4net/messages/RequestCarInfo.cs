using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net.messages
{
    public class RequestCarInfo : PluginMessage
    {
        public byte CarId { get; set; }

        public RequestCarInfo()
            :base(kunos.ACSProtocol.MessageType.ACSP_GET_CAR_INFO)
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
