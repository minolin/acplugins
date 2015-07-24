using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using acPlugins4net.kunos;
namespace acPlugins4net.messages
{
    public class MsgCarInfo : PluginMessage
    {
        public byte CarId { get; set; }
        public bool IsConnected { get; set; }
        public string CarModel { get; set; }
        public string CarSkin { get; set; }
        public string DriverName { get; set; }
        public string DriverTeam { get; set; }
        public string DriverGuid { get; set; }


        public MsgCarInfo()
            : base(ACSProtocol.MessageType.ACSP_CAR_INFO)
        {
        }
        public override string StringRepresentation
        {
            get { throw new NotImplementedException(); }
        }

        protected internal override void Serialize(System.IO.BinaryWriter bw)
        {
            bw.Write(CarId);
            bw.Write(IsConnected);
            writeStringW(bw, CarModel);
            writeStringW(bw, CarSkin);
            writeStringW(bw, DriverName);
            writeStringW(bw, DriverTeam);
            writeStringW(bw, DriverGuid);
        }

        protected internal override void Deserialize(System.IO.BinaryReader br)
        {
            CarId = br.ReadByte();
            IsConnected = br.ReadBoolean();
            
            CarModel = readStringW(br);
            DriverName = readStringW(br);
            DriverTeam = readStringW(br);
            DriverGuid = readStringW(br);
        }
    }
}
