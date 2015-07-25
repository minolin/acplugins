using acPlugins4net;
using acPlugins4net.helpers;
using acServerFake.viemodel.messages;
using System.Collections.Generic;
using System.Linq;

namespace acServerFake.viemodel
{
    class SessionViewModel : NotifiableViewModel
    {
        private ServerViewModel Server { get; set; }
        public List<object> Messages { get; set; }
        private object _ActiveMessage;
        public object ActiveMessage
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

        public SessionViewModel(ServerViewModel serverVM)
        {
            Server = serverVM;
            Messages = InitMessages();
        }

        private List<object> InitMessages()
        {
            return new object[] {
                new NewSessionViewModel(),
                new CollisionWithEnvironmentViewModel(),
                new CollisionWithCarViewModel(),
                new CarInfoViewModel(),
            }.ToList();
        }
    }
}
