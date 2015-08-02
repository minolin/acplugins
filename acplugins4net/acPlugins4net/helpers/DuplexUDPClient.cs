using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        public delegate void ErrorHandlerDelegate(Exception ex);
        private ErrorHandlerDelegate ErrorHandler;

        public void Open(int listeningPort, int remotePort, MessageReceivedDelegate callback, ErrorHandlerDelegate errorhandler)
        {
            if (_plugin != null)
                throw new Exception("UdpServer was already started.");

            MessageReceived = callback;
            ErrorHandler = errorhandler;

            _plugin = new UdpClient(listeningPort);
            _plugin.Connect("127.0.0.1", remotePort);

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        UdpReceiveResult data = await _plugin.ReceiveAsync();
                        Task.Factory.StartNew(() =>
                        {
                            MessageReceived(data.Buffer);
                        });
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler(ex);
                    }
                }
            });
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
