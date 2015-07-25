using acServerFake.viemodel.messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acServerFake.viemodel
{
    class MainViewModel
    {
        public ServerViewModel ServerViewModel { get; set; }
        public SessionViewModel MessagesViewModel { get; set; }

        public MainViewModel()
        {
            ServerViewModel = new ServerViewModel();
            ServerViewModel.Init();
            MessagesViewModel = new SessionViewModel(ServerViewModel);
        }
    }
}
