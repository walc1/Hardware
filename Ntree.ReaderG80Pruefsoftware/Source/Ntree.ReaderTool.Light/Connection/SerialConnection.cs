using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using Shared;

namespace Ntree.ReaderTool.Light.Connection
{
    class SerialConnection : IConnection
    {
        private SerialPort _port;
        private readonly List<byte> _recBuffer = new List<byte>();
        private readonly object _locker = new object();
        private Thread _readWorker;
        private CancellationTokenSource _cancellation;
        private int _baudrate;
        private string _portname;
        public event MessageReceived MessageReceived;

        public SerialConnection()
        {
            Portname = AvailablePorts.FirstOrDefault();
            Baudrate = 115200;
        }

        public int Baudrate
        {
            get { return _baudrate; }
            set
            {
                _baudrate = value;
                if (_port != null)
                {
                    _port.BaudRate = value;
                }
            }
        }

        public int[] AvailableBaudrates => new[] {9600, 19200, 38400, 57200, 115200};
        public string[] AvailablePorts => SerialPort.GetPortNames();

        public string Portname
        {
            get { return _portname; }
            set
            {
                _portname = value;
                lock (_locker)
                {
                    if (_port != null && _port.IsOpen)
                    {
                        _port.Close();

                        _port.PortName = value;
                        _port.Open();
                    }
                }
            }
        }

        public bool IsConnected { get; set; }
        public string Name => "Serial";

        public bool Send(byte[] data)
        {
            lock (_locker)
            {
                _port.Write(data, 0, data.Length);
                return true;
            }
        }

        public byte[] SendReceive(byte[] data, int timoutMs = 1000)
        {
            lock (_locker)
            {
                var sw = Stopwatch.StartNew();
                _port.Write(data, 0, data.Length);
                var res = TryReceiveCommand(timoutMs);
                Debug.Print("SendReceice: "+ sw.ElapsedMilliseconds + "ms -> Send: " + data[0]);
                return res;
            }
        }

        public bool Connect()
        {
            try
            {
                lock (_locker)
                {
                    _port = new SerialPort(Portname, Baudrate);
                    _port.Open();
                    _port.DiscardInBuffer();
                    _port.DiscardOutBuffer();
                    _cancellation = new CancellationTokenSource();
                    _readWorker = new Thread(ReadWorker);
                    _readWorker.Start();
                    IsConnected = true;
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        public bool Disconnect()
        {
            try
            {
                _cancellation?.Cancel();
                _readWorker?.Join(2000);
                _port?.Close();
                IsConnected = false;
                return true;
            }
            catch
            {
            }
            return false;
        }

        private byte[] TryReceiveCommand(int timeout)
        {
            var t = timeout;
            while (t > 0)
            {
                while (_port.BytesToRead > 0)
                {
                    var buffer = (byte)_port.ReadByte();
                    _recBuffer.Add(buffer);
                    if (_recBuffer.Last() == Protocol.END)
                    {
                        var result =  _recBuffer.SkipWhile(x => x == '\0').ToArray();
                        _recBuffer.Clear();
                        return result;
                    }
                }
                t--;
                if (t > 0)
                {
                    Thread.Sleep(1);
                }
            }
            return null;
        }

        private void ReadWorker()
        {
            while (!_cancellation.IsCancellationRequested)
            {
                lock (_locker)
                {
                    var data = TryReceiveCommand(1);
                    if (data != null)
                    {
                        MessageReceived?.BeginInvoke(this, data, null, null);
                    }
                }
            }
        }
    }
}
