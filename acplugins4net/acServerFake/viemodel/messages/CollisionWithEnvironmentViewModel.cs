using acPlugins4net;
using acPlugins4net.kunos;
using acPlugins4net.messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acServerFake.viemodel.messages
{
    class CollisionWithEnvironmentViewModel : BaseMessageViewModel<MsgClientEvent>
    {
        public override string MsgCaption
        {
            get { return "Collision (env)"; }
        }

        public CollisionWithEnvironmentViewModel()
        {
            Message.CarId = 12;
            Message.RelativePosition = PluginMessage.Vector3f.RandomSmall();
            Message.WorldPosition = PluginMessage.Vector3f.RandomBig();
            Message.RelativeVelocity = 15.0f;
            Message.Subtype = (byte)ACSProtocol.MessageType.ACSP_CE_COLLISION_WITH_ENV;
        }
    }
}
