using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using acPlugins4net.kunos;
namespace acPlugins4net.messages
{
    public class MsgClientEvent : PluginMessage
    {
        public byte Subtype { get; set; }
        public byte CarId { get; set; }
        public byte OtherCarId { get; set; }

        public float RelativeVelocity { get; set; }
        public Vector3f WorldPosition { get; set; }
        public Vector3f RelativePosition { get; set; }

        public MsgClientEvent(ACSProtocol.MessageType type)
            : base(type)
        {
            
        }
        public override string StringRepresentation
        {
            get { return "Car=4" + NL + "OtherCar=2" + NL + "some further values I won't mock now"; }
        }

        protected internal override void Serialize(System.IO.BinaryWriter bw)
        {
            bw.Write(Subtype);
            bw.Write(CarId);
            if (Subtype == (byte)ACSProtocol.MessageType.ACSP_CE_COLLISION_WITH_CAR)
                bw.Write(OtherCarId);
            
            bw.Write(RelativeVelocity);

            writeVector3f(bw, WorldPosition);
            writeVector3f(bw, RelativePosition);
        }

        protected internal override void Deserialize(System.IO.BinaryReader br)
        {
            Subtype = br.ReadByte();
            CarId = br.ReadByte();
            if (Subtype == (byte)ACSProtocol.MessageType.ACSP_CE_COLLISION_WITH_CAR)
                OtherCarId = br.ReadByte();
            RelativeVelocity = br.ReadSingle();
            WorldPosition = readVector3f(br);
            RelativePosition = readVector3f(br);
        }
    }
}
