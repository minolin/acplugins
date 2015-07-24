using acPlugins4net.helpers;
using acServerFake.netcode;
using acServerFake.view.logviewer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acServerFake.viemodel.messages
{
    abstract class BaseMessageViewModel : NotifiableViewModel
    {
        protected string nl = Environment.NewLine;
        public DuplexUDPClient UDP { get; set; }
        public abstract string MsgName { get; }
        public RelayCommand SendCommand { get; private set; }

        public abstract byte[] GenerateBinaryCommand();
        public abstract string CreateStringRepresentation();

        public BaseMessageViewModel(DuplexUDPClient udp)
        {
            UDP = udp;

            SendCommand = new RelayCommand("Send", (p) =>
            {
                var binary = GenerateBinaryCommand();
                var s = CreateStringRepresentation();
                UDP.TrySend(binary);

                AwesomeViewerStolenFromTheInternet.CreateOutgoingLog(MsgName, s, binary);
            });
        }
    }
}
