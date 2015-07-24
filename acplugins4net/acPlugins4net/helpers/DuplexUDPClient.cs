using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace acPlugins4net.helpers
{
    public class DuplexUDPClient
    {
        private UdpClient _plugin = null;
        public delegate void MessageReceivedDelegate(byte[] data);
        private MessageReceivedDelegate MessageReceived;

        public void Open(int listeningPort, int remotePort, MessageReceivedDelegate callback)
        {
            if (_plugin != null)
                throw new Exception("UdpServer was already started.");

            MessageReceived = callback;

            _plugin = new UdpClient(listeningPort);
            _plugin.Connect("127.0.0.1", remotePort);

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    UdpReceiveResult data = await _plugin.ReceiveAsync();
                    MessageReceived(data.Buffer);
                }
            });
        }

        public void TrySend(byte[] typeByte)
        {
            if (_plugin == null)
                throw new Exception("TrySend: UdpClient missing, please open first");
            {
                _plugin.SendAsync(typeByte, typeByte.Length);
            }
        }

        public void TrySend(PluginMessage msg)
        {
            TrySend(msg.ToBinary());
        }
    }
}
