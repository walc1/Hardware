using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Shared;

namespace TestServer.Connection
{
    public class UdpConnection : IConnection
    {
        private CancellationTokenSource _cancellationToken;
        private readonly ILogger _logger;
        private EndPoint _serverEndpoint;
        private EndPoint _listenerEndPoint;
        private Socket _socket;
        private Thread _readerTask;
        private readonly object _readLocker = new object();
        private readonly object _lockSendReceive = new object();

        public UdpConnection(ILogger logger)
        {
            _logger = logger;
            Name = "Ethernet";
        }

        public event MessageReceived MessageReceived;

        public string TerminalIp { get; set; } = "192.168.1.164";

        public int Port { get; set; } = 9999;

        public bool IsConnected { get; private set; }
        public string Name { get; private set; }

        public bool Send(byte[] data)
        {
            try
            {
                _socket.SendTo(data, _serverEndpoint);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogException("Send failed", e);
            }
            return false;
        }

        public byte[] SendReceive(byte[] data, int timoutMs = 1000)
        {
            lock (_lockSendReceive)
            {
                Send(data);
                return TryReadTillEnd(timoutMs);
            }
        }

        public bool Connect()
        {
            try
            {
                _cancellationToken = new CancellationTokenSource();
                _serverEndpoint = new IPEndPoint(IPAddress.Parse(TerminalIp), Port);
                _listenerEndPoint = new IPEndPoint(IPAddress.Any, Port);

                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _socket.Bind(_listenerEndPoint);

                _readerTask = new Thread(() =>
                {
                    while (!_cancellationToken.IsCancellationRequested)
                    {
                        lock (_readLocker)
                        {
                            var data = TryReadTillEnd(1);
                            if (data != null)
                            {
                                MessageReceived?.Invoke(this, data);
                            }

                        }
                    }
                });
                _readerTask.Start();
                IsConnected = true;
                return true;
            }
            catch (Exception e)
            {
                _logger.LogException("Udp Connect failed", e);
                return false;
            }
        }

        public bool Disconnect()
        {
            _cancellationToken.Cancel();
            _readerTask.Join(1000);
            _socket.Close();
            IsConnected = false;
            return true;
        }

        private byte[] TryReadTillEnd(int timeout)
        {
            var end = DateTime.Now.AddMilliseconds(timeout);
            while (DateTime.Now < end)
            {
                if (_socket.Available > 0)
                {
                    byte[] inBuf = new byte[_socket.Available];
                    EndPoint recEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    _socket.ReceiveFrom(inBuf, ref recEndPoint);
                    if (inBuf.Last() == Protocol.END)
                    {
                        return inBuf;
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
            return null;
        }
    }
}