using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net.messages
{
    public class MsgNewConnection : PluginMessage
    {
        public string DriverName { get; set; }
        public string DriverGuid { get; set; }
        public byte CarId { get; set; }
        public string CarModel { get; set; }
        public string CarSkin { get; set; }

        public MsgNewConnection()
            : base(kunos.ACSProtocol.MessageType.ACSP_NEW_CONNECTION)
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
