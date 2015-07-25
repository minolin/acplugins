using acPlugins4net;
using acPlugins4net.helpers;
using acServerFake.view.logviewer;
using System;

namespace acServerFake.viemodel.messages
{
    public abstract class BaseMessageViewModel<T> : NotifiableViewModel where T : PluginMessage
    {
        public T Message { get; private set; }

        public abstract string MsgCaption { get; }
        public RelayCommand SendCommand { get; private set; }

        public BaseMessageViewModel()
        {
            Message = (T)Activator.CreateInstance(typeof(T));

            SendCommand = new RelayCommand("Send", (p) =>
            {
                ServerViewModel.Instance.SendMessage(Message);
            });
        }
    }
}
