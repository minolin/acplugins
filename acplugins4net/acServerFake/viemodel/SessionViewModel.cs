using acPlugins4net.helpers;
using acServerFake.netcode;
using acServerFake.viemodel.messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acServerFake.viemodel
{
    class SessionViewModel : NotifiableViewModel
    {
        private DuplexUDPClient UDPServer { get; set; }
        public List<BaseMessageViewModel> Messages { get; set; }
        private BaseMessageViewModel _ActiveMessage;
        public BaseMessageViewModel ActiveMessage
        {
            get
            {
                return _ActiveMessage;
            }
            set
            {
                _ActiveMessage = value;
                OnPropertyChanged("ActiveMessage");
            }
        }

        public SessionViewModel(DuplexUDPClient bidirectionalUDPServer)
        {
            UDPServer = bidirectionalUDPServer;
            Messages = InitMessages();
        }

        private List<BaseMessageViewModel> InitMessages()
        {
            return new BaseMessageViewModel[] {
                new NewSessionViewModel(UDPServer),
                new CollisionWithCarViewModel(UDPServer),
            }.ToList();
        }
    }
}
