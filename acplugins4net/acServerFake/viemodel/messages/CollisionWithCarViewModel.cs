using acPlugins4net.helpers;
using acPlugins4net.kunos;
using acPlugins4net.messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using acPlugins4net;

namespace acServerFake.viemodel.messages
{
    public class CollisionWithCarViewModel : BaseMessageViewModel<MsgClientEvent>
    {
        public bool DoubleSend { get; set; } = true;

        public override string MsgCaption
        {
            get { return "Collision (cars)"; }
        }

        public CollisionWithCarViewModel()
        {
            Message.CarId = 12;
            Message.OtherCarId = 7;
            Message.RelativePosition = PluginMessage.Vector3f.RandomSmall();
            Message.WorldPosition = PluginMessage.Vector3f.RandomBig();
            Message.RelativeVelocity = 15.0f;
            Message.Subtype = (byte)ACSProtocol.MessageType.ACSP_CE_COLLISION_WITH_CAR;
        }

        public override void SendMessage(MsgClientEvent Message)
        {
            base.SendMessage(Message);
            if(DoubleSend)
            {
                var copy = new MsgClientEvent(Message);
                copy.CarId = Message.OtherCarId;
                copy.OtherCarId = Message.CarId;
                ServerViewModel.Instance.SendMessage(copy);
            }
        }
    }
}
