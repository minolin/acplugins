using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace acPlugins4net.helpers
{
    public class DuplexUDPClient
    {
        private UdpClient _plugin = null;
        public delegate void MessageReceivedDelegate(byte[] data);
        private MessageReceivedDelegate MessageReceived;
        public delegate void ErrorHandlerDelegate(Exception ex);
        private ErrorHandlerDelegate ErrorHandler;
        private IPEndPoint RemoteIpEndPoint = null;

        public void Open(int listeningPort, int remotePort, MessageReceivedDelegate callback, ErrorHandlerDelegate errorhandler)
        {
            if (_plugin != null)
                throw new Exception("UdpServer was already started.");

            MessageReceived = callback;
            ErrorHandler = errorhandler;

            _plugin = new UdpClient(listeningPort);
            _plugin.Connect("127.0.0.1", remotePort);
            RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, remotePort);

            _plugin.BeginReceive(ReceiveData, null);
        }

        private void ReceiveData(IAsyncResult ar)
        {
            var bytesReceived = _plugin.EndReceive(ar, ref RemoteIpEndPoint);
            _plugin.BeginReceive(ReceiveData, null);
            new Thread(() =>
            {
                try
                {
                    MessageReceived(bytesReceived);
                }
                catch (Exception ex)
                {
                    ErrorHandler(ex);
                }
            }).Start();
        }

        public bool TrySend(byte[] typeByte)
        {
            if (_plugin == null)
                throw new Exception("TrySend: UdpClient missing, please open first");
            {
                _plugin.SendAsync(typeByte, typeByte.Length);
                return true;
            }
        }
    }
}
