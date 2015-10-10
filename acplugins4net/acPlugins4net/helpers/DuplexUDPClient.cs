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
        private readonly Queue<TimestampedBytes> _messageQueue = new Queue<TimestampedBytes>();
        private readonly Queue<TimestampedBytes> _sendMessageQueue = new Queue<TimestampedBytes>();
        public delegate void MessageReceivedDelegate(TimestampedBytes data);
        private MessageReceivedDelegate MessageReceived;
        public delegate void ErrorHandlerDelegate(Exception ex);
        private ErrorHandlerDelegate ErrorHandler;
        private IPEndPoint RemoteIpEndPoint = null;
        private Thread _processMessagesThread;
        private Thread _receiveMessagesThread;
        private Thread _sendMessagesThread;

        public int MinWaitMsBetweenSend { get; set; }

        public bool Opened { get; private set; }

        public void Open(int listeningPort, string remoteHostname, int remotePort, MessageReceivedDelegate callback, ErrorHandlerDelegate errorhandler, int minWaitMsBetweenSend = 20)
        {
            if (_plugin != null)
                throw new Exception("UdpServer was already started.");

            MinWaitMsBetweenSend = minWaitMsBetweenSend;
            MessageReceived = callback;
            ErrorHandler = errorhandler;

            _plugin = new UdpClient(listeningPort);
            _plugin.Connect(remoteHostname, remotePort);
            RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, remotePort);
            Opened = true;

            _processMessagesThread = new Thread(ProcessMessages) { Name = "ProcessMessages" };
            _processMessagesThread.Start();

            _receiveMessagesThread = new Thread(ReceiveMessages) { Name = "ReceiveMessages" };
            _receiveMessagesThread.Start();

            _sendMessagesThread = new Thread(SendMessages) { Name = "SendMessages" };
            _sendMessagesThread.Start();
        }

        public void Close()
        {
            if (_plugin != null)
            {
                lock (_sendMessageQueue)
                {
                    lock (_messageQueue)
                    {
                        Opened = false;
                        Monitor.Pulse(_messageQueue); // if the ProcessMessages thread is waiting, wake it up
                        Monitor.Pulse(_sendMessageQueue); // if the SendMessages thread is waiting, wake it up
                    }
                }

                _plugin.Close(); // _plugin.Receive in ReceiveMessages thread should return at this point

                if (Thread.CurrentThread != _processMessagesThread)
                {
                    _processMessagesThread.Join(); // make sure thread has terminated
                }
                _receiveMessagesThread.Join(); // make sure thread has terminated
                _sendMessagesThread.Join(); // make sure thread has terminated

                _plugin = null;
                _messageQueue.Clear();
                _sendMessageQueue.Clear();
            }
        }

        private void ProcessMessages()
        {
            while (Opened)
            {
                TimestampedBytes msgData;
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
            while (Opened)
            {
                try
                {
                    var bytesReceived = _plugin.Receive(ref RemoteIpEndPoint);
                    var tsb = new TimestampedBytes(bytesReceived);
                    lock (_messageQueue)
                    {
                        _messageQueue.Enqueue(tsb);
                        Monitor.Pulse(_messageQueue);
                    }
                }
                catch (Exception ex)
                {
                    if (Opened)
                    {
                        // it seems the acServer is not running/ready yet
                        ErrorHandler(ex);
                    }
                }
            }
        }

        private void SendMessages()
        {
            while (Opened)
            {
                byte[] msgData;
                lock (_sendMessageQueue)
                {
                    if (_sendMessageQueue.Count == 0)
                    {
                        if (!Opened) break; // don't start waiting and exit loop if closed
                        Monitor.Wait(_sendMessageQueue);
                        if (!Opened) break; // exit loop if closed
                    }

                    msgData = _sendMessageQueue.Dequeue().RawData;
                }

                try
                {
                    _plugin.Send(msgData, msgData.Length);
                    if (MinWaitMsBetweenSend > 0)
                    {
                        Thread.Sleep(MinWaitMsBetweenSend);
                    }
                }
                catch (Exception ex)
                {
                    ErrorHandler(ex);
                }
            }
        }

        public void Send(TimestampedBytes dgram)
        {
            if (_plugin == null)
                throw new Exception("TrySend: UdpClient missing, please open first");

            lock (_sendMessageQueue)
            {
                _sendMessageQueue.Enqueue(dgram);
                Monitor.Pulse(_sendMessageQueue);
            }
        }

        public bool TrySend(TimestampedBytes dgram)
        {
            try
            {
                Send(dgram);
                return true;// we don't really know if it worked
            }
            catch (Exception ex)
            {
                ErrorHandler(ex);
                return false;
            }
        }
    }
}
