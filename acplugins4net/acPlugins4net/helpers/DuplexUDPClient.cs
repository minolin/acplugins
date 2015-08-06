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
        private readonly Queue<byte[]> _messageQueue = new Queue<byte[]>();
        public delegate void MessageReceivedDelegate(byte[] data);
        private MessageReceivedDelegate MessageReceived;
        public delegate void ErrorHandlerDelegate(Exception ex);
        private ErrorHandlerDelegate ErrorHandler;
        private IPEndPoint RemoteIpEndPoint = null;
        private Thread _processMessagesThread;
        private Thread _receiveMessagesThread;

        public bool Opened { get; private set; }

        public void Open(int listeningPort, string remostHostname, int remotePort, MessageReceivedDelegate callback, ErrorHandlerDelegate errorhandler)
        {
            if (_plugin != null)
                throw new Exception("UdpServer was already started.");

            MessageReceived = callback;
            ErrorHandler = errorhandler;

            _plugin = new UdpClient(listeningPort);
            _plugin.Connect(remostHostname, remotePort);
            RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, remotePort);
            Opened = true;

            _processMessagesThread = new Thread(ProcessMessages) { Name = "ProcessMessages" };
            _processMessagesThread.Start();

            _receiveMessagesThread = new Thread(ReceiveMessages) { Name = "ReceiveMessages" };
            _receiveMessagesThread.Start();
        }

        public void Close()
        {
            if (_plugin != null)
            {
                lock (_messageQueue)
                {
                    Opened = false;
                    Monitor.Pulse(_messageQueue); // if the ProcessMessages thread is waiting, wake it up
                }

                _plugin.Close(); // _plugin.Receive in ReceiveMessages thread should return at this point

                _processMessagesThread.Join(); // make sure thread has terminated
                _receiveMessagesThread.Join(); // make sure thread has terminated

                _plugin = null;
                _messageQueue.Clear();
            }
        }

        private void ProcessMessages()
        {
            while (Opened)
            {
                byte[] msgData;
                lock (_messageQueue)
                {
                    if (_messageQueue.Count == 0)
                    {
                        if (!Opened) break; // don't start waiting and exit loop if closed
                        Monitor.Wait(_messageQueue);
                        if (!Opened) break; // exit loop if closed
                    }

                    msgData = _messageQueue.Dequeue();
                }

                try
                {
                    MessageReceived(msgData);
                }
                catch (Exception ex)
                {
                    ErrorHandler(ex);
                }
            }
        }

        private void ReceiveMessages()
        {
            try
            {
                while (Opened)
                {
                    var bytesReceived = _plugin.Receive(ref RemoteIpEndPoint);
                    lock (_messageQueue)
                    {
                        _messageQueue.Enqueue(bytesReceived);
                        Monitor.Pulse(_messageQueue);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Opened)
                {
                    // something has gone wrong badly
                    ErrorHandler(ex);
                }
            }
        }

        public bool TrySend(byte[] typeByte)
        {
            if (_plugin == null)
                throw new Exception("TrySend: UdpClient missing, please open first");
            {
                _plugin.Send(typeByte, typeByte.Length);
                return true;
            }
        }
    }
}
