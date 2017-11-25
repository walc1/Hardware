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
        private EndPoint _terminalEndpoint;
        private EndPoint _listenerEndPoint;
        private Socket _socket;
        private Thread _readerTask;
        private readonly object _readLocker = new object();

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
                _socket.SendTo(data, _terminalEndpoint);
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
            lock (_readLocker)
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
                _terminalEndpoint = new IPEndPoint(IPAddress.Parse(TerminalIp), Port);
                _listenerEndPoint = new IPEndPoint(IPAddress.Any, Port);

                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _socket.Bind(_listenerEndPoint);

                _readerTask = new Thread(() =>
                {
                    while (!_cancellationToken.IsCancellationRequested)
                    {
                        byte[] data;

                        lock (_readLocker)
                        {
                            data = TryReadTillEnd(1);
                        }

                        if (data != null)
                        {
                            MessageReceived?.Invoke(this, data);
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
            try
            {
                var end = DateTime.Now.AddMilliseconds(timeout);
                while (DateTime.Now < end)
                {
                    if (_socket.Available > 0)
                    {
                        byte[] inBuf = new byte[_socket.Available];
                        // Note: Dies m�sste das Terminal sein von dem die Message kommt, oder?
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
            }
            catch (Exception e)
            {
                _logger.LogException("TryReadTillEnd failed", e);
                return null;
            }

            return null;
        }
    }
}