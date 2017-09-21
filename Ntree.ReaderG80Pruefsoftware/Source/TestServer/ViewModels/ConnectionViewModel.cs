using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using Shared;
using TestServer.Connection;
using TestServer.Properties;

namespace TestServer.ViewModels
{
    class ConnectionViewModel : Screen
    {
        private IConnection _connection;
        private bool _isSerialConnection;
        private bool _isNetworkConnection;
        private SerialConnection _serial;
        private UdpConnection _udp;

        public ConnectionViewModel(ILogger logger)
        {
            DisplayName = "Connection";
            Connections = new List<IConnection>();
            _serial = new SerialConnection();
            if (_serial.AvailablePorts.Any(x => x.Equals(Settings.Default.SerialPort, StringComparison.InvariantCultureIgnoreCase)))
            {
                _serial.Portname = Settings.Default.SerialPort;
            }
            _serial.Baudrate = Settings.Default.Baudrate;

            Connections.Add(_serial);
            _udp = new UdpConnection(logger);
            _udp.TerminalIp = Settings.Default.TerminalIp;
            _udp.Port = Settings.Default.NetworkPort;
            Connections.Add(_udp);

            if (Settings.Default.UseSerialPort)
            {
                IsSerialConnection = true;
            }
            else
            {
                IsNetworkConnection = true;
            }
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            Settings.Default.SerialPort = _serial.Portname;
            Settings.Default.Baudrate = _serial.Baudrate;
            Settings.Default.TerminalIp = _udp.TerminalIp;
            Settings.Default.NetworkPort = _udp.Port;
            Settings.Default.UseSerialPort = IsSerialConnection;
            Settings.Default.Save();
        }

        public List<IConnection> Connections { get; set; }

        public IConnection Connection
        {
            get { return _connection; }
            set
            {
                if (Equals(value, _connection)) return;
                _connection = value;
                NotifyOfPropertyChange(nameof(Connection));
                NotifyOfPropertyChange(nameof(CanConnect));
            }
        }

        public bool CanConnect => Connection != null;

        public bool IsSerialConnection
        {
            get { return _isSerialConnection; }
            set
            {
                _isSerialConnection = value;
                if(value)
                    Connection = Connections.OfType<SerialConnection>().FirstOrDefault();
            }
        }

        public bool IsNetworkConnection
        {
            get { return _isNetworkConnection; }
            set
            {
                _isNetworkConnection = value;
                if(value)
                    Connection = Connections.OfType<UdpConnection>().FirstOrDefault();
            }
        }

        public void Connect()
        {
            Connection.Connect();
            TryClose(true);
        }

        public void Cancel()
        {
            TryClose(false);
        }
    }
}
