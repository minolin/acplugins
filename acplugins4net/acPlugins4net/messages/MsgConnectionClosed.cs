using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net.messages
{
    public class MsgConnectionClosed : PluginMessage
    {
        public string DriverName { get; set; }
        public string DriverGuid { get; set; }
        public byte CarId { get; set; }
        public string CarModel { get; set; }
        public string CarSkin { get; set; }

        public MsgConnectionClosed()
            : base(kunos.ACSProtocol.MessageType.ACSP_CONNECTION_CLOSED)
        {

        }

        protected internal override void Deserialize(BinaryReader br)
        {
            DriverName = readStringW(br);
            DriverGuid = readStringW(br);
            CarId = br.ReadByte();
            CarModel = readString(br);
            CarSkin = readString(br);
        }

        protected internal override void Serialize(BinaryWriter bw)
        {
            writeStringW(bw, DriverName);
            writeStringW(bw, DriverGuid);
            bw.Write(CarId);
            writeStringW(bw, CarModel);
            writeStringW(bw, CarSkin);
        }
    }

}
