using acPlugins4net.helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net.messages
{
    public class MsgCarUpdate : PluginMessage
    {
        #region As-binary-members; we should reuse them exactly this way to stay efficient

        public byte CarId { get; set; }
        public Vector3f WorldPosition { get; set; }
        public Vector3f Velocity { get; set; }
        public byte Gear { get; set; }
        public ushort EngineRPM { get; set; }
        public float NormalizedSplinePosition { get; set; }

        #endregion

        public MsgCarUpdate()
            :base(kunos.ACSProtocol.MessageType.ACSP_CAR_UPDATE)
        {

        }

        protected internal override void Deserialize(BinaryReader br)
        {
            CarId = br.ReadByte();
            WorldPosition = readVector3f(br);
            Velocity = readVector3f(br);
            Gear = br.ReadByte();
            EngineRPM = br.ReadUInt16();
            NormalizedSplinePosition = br.ReadSingle();
        }

        protected internal override void Serialize(BinaryWriter bw)
        {
            bw.Write(CarId);
            writeVector3f(bw, WorldPosition);
            writeVector3f(bw, Velocity);
            bw.Write(Gear);
            bw.Write(EngineRPM);
            bw.Write(NormalizedSplinePosition);
        }
    }
}
