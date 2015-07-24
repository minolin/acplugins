using acPlugins4net.helpers;
using acPlugins4net.kunos;
using acPlugins4net.messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acServerFake.viemodel.messages
{
    class CollisionWithCarViewModel : BaseMessageViewModel
    {
        protected MsgClientEvent _msgClientEvent = null;

        public override string MsgName
        {
            get { return "Collision (cars)"; }
        }

        public CollisionWithCarViewModel(DuplexUDPClient UDPServer)
            : base(UDPServer)
        {
            _msgClientEvent = new MsgClientEvent(ACSProtocol.MessageType.ACSP_CE_COLLISION_WITH_CAR);
        }

        public override byte[] GenerateBinaryCommand()
        {
            return _msgClientEvent.ToBinary();
        }

        public override string CreateStringRepresentation()
        {
            return _msgClientEvent.StringRepresentation;
        }
    }
}
