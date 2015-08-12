using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using acPlugins4net.kunos;
using acPlugins4net.helpers;

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

        public MsgClientEvent()
            : base(ACSProtocol.MessageType.ACSP_CLIENT_EVENT)
        {
        }


        public MsgClientEvent(MsgClientEvent copy)
            : base(ACSProtocol.MessageType.ACSP_CLIENT_EVENT)
        {
            Subtype = copy.Subtype;
            CarId = copy.CarId;
            OtherCarId = copy.OtherCarId;
            RelativeVelocity = copy.RelativeVelocity;
            WorldPosition = copy.WorldPosition; // wrong, should really copy this
            RelativePosition = copy.RelativePosition;// wrong, should really copy this
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
