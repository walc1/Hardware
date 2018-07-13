using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Caliburn.Micro;
using Microsoft.Win32;
using Shared;
using Color = System.Windows.Media.Color;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace Ntree.ReaderTool.Light.ViewModels
{
    public class LogItem
    {
        public string Message { get; set; }
    }

    public class MainViewModel : Screen, IDisposable, ILogger
    {
        private IWindowManager _windowManager;
        private ProtocolManager _protocolManager;
        private List<string> _logLines = new List<string>();
        private List<string> _errorLogLines = new List<string>();

        private Protocol _protocol;
        private ProtocolHelper _protocolHelper;
        private string _terminalTime;
        private byte _index;
        private byte[] _resultData;

        private string _terminalVersion = "-.-.-.-";
        private ConnectionViewModel _connectionVm;
        private string _terminalMacAddress = "xx-xx-xx-xx-xx-xx";
        private System.Windows.Threading.DispatcherTimer _TimerAliveSignal;


        public MainViewModel()
        {
            _windowManager = new WindowManager();
            _protocol = new Protocol(this);
            _protocol.DateTimeChange += (sender, time) =>
            {
                TerminalTime = time.ToString("T");
            };

            _protocol.SystemInfoChanged += OnSystemInfoChanged;


            TerminalTime = "--:--:--";
            _protocolHelper = new ProtocolHelper(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8 });

            _connectionVm = new ConnectionViewModel(this);

            _protocolManager = new ProtocolManager(_protocolHelper, _protocol, this);

            _TimerAliveSignal = new System.Windows.Threading.DispatcherTimer();
            _TimerAliveSignal.Tick += _TimerAliveSignal_Tick;
            _TimerAliveSignal.Interval = new TimeSpan(0, 0, 5);
            _TimerAliveSignal.Start();
        }

        private void _TimerAliveSignal_Tick(object sender, EventArgs e)
        {
            if (IsConnected)
            {
                var cmd = _protocol.CreateSetCommand(0, (byte)Command.ReaderToolAliveSignal); // AliveSignal);
                EncryptSendReceiveAck(cmd);
            }
        }

        private void ShowConnectionView()
        {
            if (_windowManager.ShowDialog(_connectionVm) == true)
            {
                Connection = _connectionVm.Connection;
                Connection.MessageReceived += ConnectionOnMessageReceived;
            }
            NotifyOfPropertyChange(nameof(IsConnected));
        }

        private void OnSystemInfoChanged(object o, Version version, byte[] macAddress)
        {
            TerminalVersion = version.ToString();
            TerminalMacAddress = string.Join("-", macAddress.Select(x => x.ToString("X2")));
        }

        public bool IsConnected => Connection?.IsConnected ?? false;

        public string TerminalVersion
        {
            get { return _terminalVersion; }
            set
            {
                if (value == _terminalVersion) return;
                _terminalVersion = value;
                NotifyOfPropertyChange(nameof(TerminalVersion));
            }
        }

        public string TerminalMacAddress
        {
            get { return _terminalMacAddress; }
            set
            {
                if (value == _terminalMacAddress) return;
                _terminalMacAddress = value;
                NotifyOfPropertyChange(nameof(TerminalMacAddress));
            }
        }

        private IConnection _Connection;

        public IConnection Connection
        {
            get { return _Connection; }
            set
            {
                _Connection = value;
                _protocolManager.Connection = value;
            }
        }

        public void Connect()
        {
            Connection?.Disconnect();
            ShowConnectionView();
            ClearLog();
        }

        public void Disconnect()
        {
            if (Connection != null)
            {
                Connection.MessageReceived -= ConnectionOnMessageReceived;
                Connection.Disconnect();
            }
            NotifyOfPropertyChange(nameof(IsConnected));
        }

        public string LogText
        {
            get
            {
                string result = "";
                foreach (var item in _logLines)
                {
                    result += item;
                }
                return result;
            }
        }

        public string ErrorLogText
        {
            get
            {
                string result = "";
                foreach (var item in _errorLogLines)
                {
                    result += item;
                }
                return result;
            }
        }

        public string TerminalTime
        {
            get { return _terminalTime; }
            set
            {
                if (value == _terminalTime) return;
                _terminalTime = value;
                NotifyOfPropertyChange(nameof(TerminalTime));
            }
        }

        public void SetTime()
        {
            var timeNow = DateTime.Now;
            var cmd = _protocol.CreateTimeCommand(timeNow);
            var result = EncryptSendReceiveAck(cmd);

            if (result == ProtocolResult.AckAck)
            {
                AddLog($"Set Time to {timeNow.ToString()}");
            }
        }

        public void SendReboot()
        {
            var cmd = _protocol.CreateRebootCommand();
            EncryptSendReceice(cmd);

            ClearLog();
            AddLog("Call Terminal reboot...");
        }

        public void ReadTime()
        {
            var cmd = _protocol.CreateTimeRequestCommand();
            EncryptSendReceice(cmd);
        }

        public void ReadSystemInfo()
        {
            var cmd = _protocol.CreateVersionRequestCommand();
            EncryptSendReceice(cmd);
        }

        private void EncryptSendReceice(byte[] data)
        {
            _protocolManager.EncryptSendReceice(data);
        }

        private ProtocolResult EncryptSendReceiveAck(byte[] msg)
        {
            return _protocolManager.EncryptSendReceiveAck(msg);
        }

        private object _LockAddLogg = new object();
        private object _LockAddErrorLogg = new object();

        public void ClearLog()
        {
            _logLines.Clear();
            NotifyOfPropertyChange(nameof(LogText));
        }

        public void AddLog(string text)
        {
            lock (_LockAddLogg)
            {
                while (_logLines.Count > 200)
                {
                    _logLines.RemoveAt(_logLines.Count - 1);
                }

                _logLines.Insert(0, $"{DateTime.Now}: {text} \n");
                NotifyOfPropertyChange(nameof(LogText));
            }
        }

        public void AddErrorLog(string text)
        {
            AddLog(text);

            lock (_LockAddErrorLogg)
            {
                while (_errorLogLines.Count > 200)
                {
                    _errorLogLines.RemoveAt(_errorLogLines.Count - 1);
                }

                _errorLogLines.Insert(0, $"{DateTime.Now}: {text} \n");

                NotifyOfPropertyChange(nameof(ErrorLogText));
            }
        }


        private void ConnectionOnMessageReceived(object o, byte[] data)
        {
            byte[] msg;
            try
            {
                msg = _protocolHelper.DecryptData(data);
            }
            catch (Exception e)
            {
                AddLog("Message decrypt error!\nException: " + e);
                return;
            }

            var res = _protocol.Parse(msg, out _index, out _resultData);
            if (res != ProtocolResult.Ack && res != ProtocolResult.None && res != ProtocolResult.AckAck)
            {
                AddLog("NACK: " + res);
            }
            else if (res == ProtocolResult.Ack)
            {
                var cmd = _protocol.CreateAck();
                Connection.Send(_protocolHelper.EncryptMessage(cmd));
            } 
        }

        public void Dispose()
        {
            Disconnect();
        }

        public void Log(string text)
        {
            AddLog(text);
        }

        public void LogError(string text)
        {
            AddLog(text);
        }

        public void LogException(Exception e)
        {
            AddLog("ERROR: " + e);
        }

        public void LogException(string text, Exception e)
        {
            AddLog("ERROR: " + text + "\n" + e);
        }
    }
}