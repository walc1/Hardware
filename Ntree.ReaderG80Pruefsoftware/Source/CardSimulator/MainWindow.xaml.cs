using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CardSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private SerialPort _serial;
        private string _text;
        private bool _isConnected;
        private bool _redirectModeEnabled;
        private string _readTemp;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public IEnumerable<string> ComPorts => SerialPort.GetPortNames();

        public string ComPort { get; set; }

        private void BtOpen_OnClick(object sender, RoutedEventArgs e)
        {
            if (!IsConnected)
            {
                _serial = new SerialPort(ComPort, 19200); // 9600
                _serial.Open();
                _serial.DiscardInBuffer();
                _serial.DataReceived += SerialOnDataReceived;
                btOpen.Content = "Close";
                Text = string.Empty;
                IsConnected = true;
            }
            else
            {
                _serial.Close();
                btOpen.Content = "Open";
                IsConnected = false;
            }
        }

        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                _isConnected = value; 
                OnPropertyChanged(nameof(IsConnected));
            }
        }

        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                OnPropertyChanged(nameof(Text));
            }
        }

        public bool RedirectModeEnabled
        {
            get { return _redirectModeEnabled; }
            set
            {
                _redirectModeEnabled = value;
            }
        }

        private void SerialOnDataReceived(object sender, SerialDataReceivedEventArgs serialDataReceivedEventArgs)
        {
            var data = _serial.ReadExisting();
            if (Text.Length > 1000)
            {
                Text = string.Empty;
            }
            Text += data;
            if (RedirectModeEnabled)
            {
                _readTemp += data;
                if (_readTemp.Length >= ReadLength)
                {
                    _serial.Write(ReturnData);
                    _readTemp = string.Empty;
                }
            }

        }

        public int ReadLength { get; set; } = 10;

        public string ReturnData { get; set; } = "HelloServer!";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void CardButton_OnClick(object sender, RoutedEventArgs e)
        {
            var bt = sender as Button;
            if (bt != null)
            {
                var data = $"#>{bt.Content}<#";
                _serial.Write(data);

            }
        }
    }
}
