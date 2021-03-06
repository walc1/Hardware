﻿using System;
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
using TestServer.AutoTest;
using System.Text.RegularExpressions;
using System.Windows.Data;
using TestServer.Connection;

namespace TestServer.ViewModels
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
        private bool _inputState1;
        private bool _inputState2;
        private bool _inputState3;
        private bool _inputState4;
        private string _touchButtonId;
        private string _imageFile;
        private string _terminalVersion = "-.-.-.-";
        private string _readMedia = "---";
        private string _readMediaRaw = "---";
        private ConnectionViewModel _connectionVm;
        private string _terminalMacAddress = "xx-xx-xx-xx-xx-xx";
        private byte _backlightValue = 127;
        private string _i2CReadResult = "-";
        private string _i2CWriteReadResult;
        private string _spiResult;
        private System.Windows.Threading.DispatcherTimer _TimerAliveSignal;

        private AutoIOTest _AutoIOTest;
        private AutoReaderTest _AutoReaderTest;
        private AutoCompleteTest _AutoCompleteTest;

        public MainViewModel()
        {
            _windowManager = new WindowManager();
            TerminaFiles = new ObservableCollection<string>();
            ImageFiles = new ObservableCollection<string>();
            _protocol = new Protocol(this);
            _protocol.DateTimeChange += (sender, time) =>
            {
                TerminalTime = time.ToString("T");
            };
            _protocol.InputChanged += OnInputChanged;
            _protocol.FileListChanged += OnFileListChanged;
            _protocol.TouchButton += OnTouchButton;
            _protocol.SystemInfoChanged += OnSystemInfoChanged;
            //_protocol.MediaRead += (sender, id, data) => ReadMedia = $"Id: {id} -> Card: {Encoding.UTF8.GetString(data)}";
            _protocol.MediaRead += _protocol_MediaRead;
            _protocol.MediaReadSector += _protocol_MediaReadSector;
            _protocol.DeviceStateChanged += OnProtocol_DeviceStateChanged;

            TerminalTime = "--:--:--";
            _protocolHelper = new ProtocolHelper(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8 });

            _connectionVm = new ConnectionViewModel(this);

            _protocolManager = new ProtocolManager(_protocolHelper, _protocol, this);
            _AutoIOTest = new AutoIOTest(_protocol, _protocolManager, this);
            _AutoReaderTest = new AutoReaderTest(_protocol, _protocolManager, this);
            _AutoCompleteTest = new AutoCompleteTest(_protocol, _protocolManager, this);

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

        private void OnProtocol_DeviceStateChanged(object sender, byte readerId, DeviceState state)
        {
            //throw new NotImplementedException();
        }

        private void _protocol_MediaReadSector(object sender, byte readerId, byte[] cardData, SectorData[] sectorData)
        {
            _protocol_MediaRead(sender, readerId, cardData);
        }

        private void _protocol_MediaRead(object sender, byte readerId, byte[] cardData)
        {
            // Validate data
            var regexItem = new Regex("^[-sa-fA-F0-9]+$");
            var dataAsString = Encoding.UTF8.GetString(cardData);
            dataAsString = dataAsString.Trim('\r', '\n', '\0');

            // ToDo: Check länge

            if (!regexItem.IsMatch(dataAsString))
            {
                LogError($"Invalid data {dataAsString} for reader id {readerId}");
                return;
            }

            ReadMedia = $"Id: {readerId} -> Card: {dataAsString}";
            ReadMediaRaw = dataAsString;

            if (readerId == 1)
            {
                ReadMedia1 = dataAsString;
            }
            else if (readerId == 2)
            {
                ReadMedia2 = dataAsString;
            }
            else if (readerId == 3)
            {
                ReadMedia3 = dataAsString;
            }
            else
            {
                AddErrorLog($"Error - invalid readerId {readerId}");
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

        private void OnTouchButton(object o, byte buttonId)
        {
            TouchButtonId = buttonId.ToString();
        }

        public string TouchButtonId
        {
            get { return _touchButtonId; }
            set
            {
                if (value == _touchButtonId) return;
                _touchButtonId = value;
                NotifyOfPropertyChange(nameof(TouchButtonId));
            }
        }

        private void OnFileListChanged(object o, string[] files)
        {
            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                TerminaFiles.Clear();
                foreach (var file in files)
                {
                    TerminaFiles.Add(file);
                }

                ImageFiles.Clear();
                var images = TerminaFiles.Where(x => Path.GetExtension(x) == ".png").ToList();
                images.ForEach(x => ImageFiles.Add(x));
                ImageFile = ImageFiles.FirstOrDefault();
            }));
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


        public bool[] RelaisStates { get; set; } = new bool[7];

        public double RelaisTime { get; set; } = 1;

        public bool InputState1
        {
            get { return _inputState1; }
            set
            {
                if (value == _inputState1) return;
                _inputState1 = value;
                NotifyOfPropertyChange(nameof(InputState1));
            }
        }

        public bool InputState2
        {
            get { return _inputState2; }
            set
            {
                if (value == _inputState2) return;
                _inputState2 = value;
                NotifyOfPropertyChange(nameof(InputState2));
            }
        }

        public bool InputState3
        {
            get { return _inputState3; }
            set
            {
                if (value == _inputState3) return;
                _inputState3 = value;
                NotifyOfPropertyChange(nameof(InputState3));
            }
        }

        public bool InputState4
        {
            get { return _inputState4; }
            set
            {
                if (value == _inputState4) return;
                _inputState4 = value;
                NotifyOfPropertyChange(nameof(InputState4));
            }
        }

        public void Connect()
        {
            Connection?.Disconnect();
            ShowConnectionView();
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

        public double BeeperTime { get; set; } = 1;

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

        public byte[] IndexTwenty => new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20};
        public byte[] IndexTen => new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        public byte[] IndexThree => new byte[] { 1, 2, 3 };

        public byte[] TextSizes => new byte[] { 26, 27, 28, 29, 30, 31, 32, 33, 34};

        public Color[] AvailableColors => new[] { Colors.Black, Colors.DarkSlateGray, Colors.SlateGray, Colors.DarkGray, Colors.LightGray, Colors.White, Colors.Red, Colors.Orange, Colors.Yellow, Colors.GreenYellow, Colors.Green, Colors.DarkCyan, Colors.Blue, Colors.Violet };

        public byte TextIndex { get; set; } = 1;
        public ushort TextPosX { get; set; }
        public ushort TextPosY { get; set; }
        public byte TextSize { get; set; } = 30;
        public Color TextColor { get; set; } = Colors.White;
        public string Text { get; set; } = "Text!";


        public byte ButtonIndex { get; set; } = 1;
        public ushort ButtonPosX { get; set; }
        public ushort ButtonPosY { get; set; }
        public ushort ButtonWidth { get; set; } = 100;
        public ushort ButtonHeight { get; set; } = 50;
        public byte ButtonTextSize { get; set; } = 29;
        public Color ButtonTextColor { get; set; } = Colors.White;
        public Color ButtonBackColor { get; set; } = Colors.Blue;
        public string ButtonText { get; set; } = "Button!";

        public byte RectangleIndex { get; set; } = 1;
        public ushort RectanglePosX { get; set; }
        public ushort RectanglePosY { get; set; }
        public ushort RectangleWidth { get; set; } = 100;
        public ushort RectangleHeight { get; set; } = 50;
        public Color RectangleColor { get; set; } = Colors.Red;

        public byte LineIndex { get; set; } = 1;
        public ushort LinePosX1 { get; set; }
        public ushort LinePosY1 { get; set; }
        public ushort LinePosX2 { get; set; } = 100;
        public ushort LinePosY2 { get; set; } = 50;
        public byte LineWidth { get; set; } = 5;
        public Color  LineColor { get; set; } = Colors.Yellow;

        public byte ImageIndex { get; set; } = 1;
        public ushort ImagePosX { get; set; }
        public ushort ImagePosY { get; set; }

        public string ImageFile
        {
            get { return _imageFile; }
            set
            {
                if (value == _imageFile) return;
                _imageFile = value;
                NotifyOfPropertyChange(nameof(ImageFile));
            }
        }

        public ObservableCollection<string> ImageFiles { get; set; }

        public Color DisplayBackColor { get; set; } = Colors.Black;

        public ObservableCollection<string> TerminaFiles { get; set; }

        public string SelectedFile { get; set; }

        public string ReadMedia
        {
            get { return _readMedia; }
            private set
            {
                if (value == _readMedia) return;
                _readMedia = value; //.TrimEnd('\n', '\r');
                NotifyOfPropertyChange(nameof(ReadMedia));
            }
        }

        public string ReadMediaRaw
        {
            get { return _readMediaRaw; }
            set
            {
                if (value == _readMediaRaw) return;
                _readMediaRaw = value; //.TrimEnd('\n', '\r');
                NotifyOfPropertyChange(nameof(ReadMediaRaw));
            }
        }

        private string _readMedia1;
        public string ReadMedia1
        {
            get { return _readMedia1; }
            private set
            {
                if (value == _readMedia1) return;             
                _readMedia1 = value; //.TrimEnd('\n', '\r');                
                _AutoReaderTest.CheckReaderMedia1(_readMedia1);
                NotifyOfPropertyChange(nameof(ReadMedia1));
            }
        }

        private string _readMedia2;
        public string ReadMedia2
        {
            get { return _readMedia2; }
            private set
            {
                if (value == _readMedia2) return;
                _readMedia2 = value; //.TrimEnd('\n', '\r');
                _AutoReaderTest.CheckReaderMedia2(_readMedia2);
                NotifyOfPropertyChange(nameof(ReadMedia2));
            }
        }

        private string _readMedia3;
        public string ReadMedia3
        {
            get { return _readMedia3; }
            private set
            {
                if (value == _readMedia3) return;
                _readMedia3 = value; //.TrimEnd('\n', '\r');
                NotifyOfPropertyChange(nameof(ReadMedia3));
            }
        }

        public byte MediaRequestIndex { get; set; } = 1;
        public byte PortRedirectIndex { get; set; } = 1;
        public ushort PortRedirectTimeout { get; set; } = 1000;
        public ushort PortRedirectReadLength { get; set; } = 10;
        public string PortRedirectWriteData { get; set; } = "HelloWorld!";

        public byte BacklightValue
        {
            get { return _backlightValue; }
            set
            {
                if (value == _backlightValue) return;
                _backlightValue = value;
                var cmd = _protocol.CreateDisplayBacklightCommand(value);
                EncryptSendReceiveAck(cmd);
                NotifyOfPropertyChange(nameof(BacklightValue));
            }
        }

        public byte I2CReadAddr { get; set; } = 81;

        public int I2CReadLength { get; set; } = 4;

        public byte I2CWriteAddr { get; set; } = 81;

        public string I2CWriteData { get; set; } = "24,1,2,3,4";

        public byte I2CWriteReadAddr { get; set; } = 81;

        public string I2CWriteReadData { get; set; } = "24";

        public int I2CWriteReadLength { get; set; } = 4;

        public string I2CReadResult
        {
            get { return _i2CReadResult; }
            set
            {
                if (value == _i2CReadResult) return;
                _i2CReadResult = value;
                NotifyOfPropertyChange(nameof(I2CReadResult));
            }
        }

        public string I2CWriteReadResult
        {
            get { return _i2CWriteReadResult; }
            set
            {
                if (value == _i2CWriteReadResult) return;
                _i2CWriteReadResult = value;
                NotifyOfPropertyChange(nameof(I2CWriteReadResult));
            }
        }

        public object SpiChipSelects => Enum.GetValues(typeof(SpiChipSelect));

        public SpiChipSelect SpiChipSelect { get; set; } = SpiChipSelect.Cs1;

        public IEnumerable<SpiSpeed> SpiSpeeds => Enum.GetValues(typeof(SpiSpeed)).Cast<SpiSpeed>();

        public SpiSpeed SpiSpeed { get; set; } = SpiSpeed.Khz100;

        public bool SpiChipselectActiveState { get; set; } = true;
        public bool SpiClockEdge { get; set; } = true;
        public bool SpiClockIdle { get; set; }
        public string SpiData { get; set; } = "1,2,3,4";

        public string SpiResult
        {
            get { return _spiResult; }
            set
            {
                if (value == _spiResult) return;
                _spiResult = value;
                NotifyOfPropertyChange(nameof(SpiResult));
            }
        }

        public void WriteReadSpi()
        {
            byte[] data = null;
            try
            {
                data = SpiData.Split(',').Select(byte.Parse).ToArray();
            }
            catch
            {
                MessageBox.Show("Invalid data! Use 0-255 and separate with ','. E.g. '24,23,0,255,1");
                return;
            }
            var cmd = _protocol.CreateSpiWriteReadCommand(SpiChipSelect, SpiChipselectActiveState, SpiClockEdge, SpiClockIdle, SpiSpeed, data);
            EncryptSendReceice(cmd);
            if (_resultData != null)
            {
                SpiResult = string.Join(",", _resultData.Select(x => x.ToString()));
            }
        }

        public void ReadI2C()
        {
            var cmd = _protocol.CreateI2CReadDataCommand(I2CReadAddr, I2CReadLength);
            EncryptSendReceice(cmd);
            I2CReadResult = string.Join(",", _resultData.Select(x => x.ToString()));
        }

        public void WriteI2C()
        {
            byte[] data = null; 
            try
            {
                data = I2CWriteData.Split(',').Select(byte.Parse).ToArray();
            }
            catch
            {
                MessageBox.Show("Invalid data! Use 0-255 and separate with ','. E.g. '24,23,0,255,1");
                return;
            }
            var cmd = _protocol.CreateI2CWriteDataCommand(I2CWriteAddr, data);
            EncryptSendReceiveAck(cmd);
        }

        public void WriteReadI2C()
        {
            byte[] data = null;
            try
            {
                data = I2CWriteReadData.Split(',').Select(byte.Parse).ToArray();
            }
            catch
            {
                MessageBox.Show("Invalid data! Use 0-255 and separate with ','. E.g. '24,23,0,255,1");
                return;
            }
            var cmd = _protocol.CreateI2CWriteReadDataCommand(I2CWriteReadAddr, data, I2CWriteReadLength);
            EncryptSendReceice(cmd);
            I2CWriteReadResult = string.Join(",", _resultData.Select(x => x.ToString()));
        }

        public void ClearReadMedia()
        {
            ReadMedia = "---";
        }

        public void PortRedirect()
        {
            var cmd = _protocol.CreatePortRedirectCommand(PortRedirectIndex, PortRedirectTimeout, PortRedirectReadLength, Encoding.UTF8.GetBytes(PortRedirectWriteData));
            EncryptSendReceice(cmd);
        }

        public void RequestReadMedia()
        {
            var cmd = _protocol.CreateRequestMediaReadCommand(MediaRequestIndex);
            EncryptSendReceice(cmd);
        }

        public void SendText()
        {
            var cmd = _protocol.CreateDisplayTextCommand(TextIndex, new ColorInfo(TextColor.R, TextColor.G, TextColor.B), (short)TextPosX, (short)TextPosY, TextSize, Text);
            EncryptSendReceiveAck(cmd);
        }

        public void DeleteText()
        {
            var cmd = _protocol.CreateDisplayDeleteCommand(TextIndex, DisplayCommand.Text);
            EncryptSendReceiveAck(cmd);
        }

        public void SendButton()
        {
            var cmd = _protocol.CreateDisplayButtonCommand(ButtonIndex, new ColorInfo(ButtonTextColor.R, ButtonTextColor.G, ButtonTextColor.B), 
                new ColorInfo(ButtonBackColor.R, ButtonBackColor.G, ButtonBackColor.B), ButtonPosX, ButtonPosY, ButtonWidth, ButtonHeight, ButtonTextSize, ButtonText);
            EncryptSendReceiveAck(cmd);
        }

        public void DeleteButton()
        {
            var cmd = _protocol.CreateDisplayDeleteCommand(ButtonIndex, DisplayCommand.Button);
            EncryptSendReceiveAck(cmd);
        }

        public void SendRectangle()
        {
            var cmd = _protocol.CreateDisplayRectangleCommand(RectangleIndex, new ColorInfo(RectangleColor.R, RectangleColor.G, RectangleColor.B), RectanglePosX, RectanglePosY, RectangleWidth, RectangleHeight);
            EncryptSendReceiveAck(cmd);
        }

        public void DeleteRectangle()
        {
            var cmd = _protocol.CreateDisplayDeleteCommand(RectangleIndex, DisplayCommand.Rectangle);
            EncryptSendReceiveAck(cmd);
        }

        public void SendLine()
        {
            var cmd = _protocol.CreateDisplayLineCommand(LineIndex, new ColorInfo(LineColor.R, LineColor.G, LineColor.B), LinePosX1, LinePosY1, LinePosX2, LinePosY2, LineWidth);
            EncryptSendReceiveAck(cmd);
        }

        public void DeleteLine()
        {
            var cmd = _protocol.CreateDisplayDeleteCommand(LineIndex, DisplayCommand.Line);
            EncryptSendReceiveAck(cmd);
        }

        public void SendImage()
        {
            if (!string.IsNullOrEmpty(ImageFile))
            {
                var cmd = _protocol.CreateDisplayImageCommand(ImageIndex, ImagePosX, ImagePosY, ImageFile);
                EncryptSendReceiveAck(cmd);
            }
        }

        public void DeleteImage()
        {
            var cmd = _protocol.CreateDisplayDeleteCommand(ImageIndex, DisplayCommand.Image);
            EncryptSendReceiveAck(cmd);
        }

        public void SendInvalidate()
        {
            var cmd = _protocol.CreateDisplayInvalidateCommand();
            EncryptSendReceiveAck(cmd);
        }

        public void SendClear()
        {
            var cmd = _protocol.CreateDisplayClearAllCommand(new ColorInfo(DisplayBackColor.R, DisplayBackColor.G, DisplayBackColor.B));
            EncryptSendReceiveAck(cmd);
        }

        public void SendRelais()
        {
            byte mask = 0;
            for (int i = 0; i < RelaisStates.Length; i++)
            {
                if (RelaisStates[i])
                {
                    mask |= (byte) Math.Pow(2, i);
                }
            }

            var cmd = _protocol.CreateRelaisCommand(mask);
            EncryptSendReceiveAck(cmd);
        }

        public void SendBeeper()
        {
            var cmd = _protocol.CreateBeeperCommand(BeeperTime);
            EncryptSendReceiveAck(cmd);
        }

        public void SendTimeRelais1()
        {
            var cmd = _protocol.CreateSingleRelaisCommand((byte)Math.Pow(2, 0), RelaisTime);
            EncryptSendReceiveAck(cmd);
        }

        public void SendTimeRelais2()
        {
            var cmd = _protocol.CreateSingleRelaisCommand((byte)Math.Pow(2, 1), RelaisTime);
            EncryptSendReceiveAck(cmd);
        }

        public void SendTimeRelais3()
        {
            var cmd = _protocol.CreateSingleRelaisCommand((byte)Math.Pow(2, 2), RelaisTime);
            EncryptSendReceiveAck(cmd);
        }

        public void SendTimeRelais4()
        {
            var cmd = _protocol.CreateSingleRelaisCommand((byte)Math.Pow(2, 3), RelaisTime);
            EncryptSendReceiveAck(cmd);
        }

        public void SendTimeRelais5()
        {
            var cmd = _protocol.CreateSingleRelaisCommand((byte)Math.Pow(2, 4), RelaisTime);
            EncryptSendReceiveAck(cmd);
        }

        public void SendTimeRelais6()
        {
            var cmd = _protocol.CreateSingleRelaisCommand((byte)Math.Pow(2, 5), RelaisTime);
            EncryptSendReceiveAck(cmd);
        }

        public void SendTimeRelais7()
        {
            var cmd = _protocol.CreateSingleRelaisCommand((byte)Math.Pow(2, 6), RelaisTime);
            EncryptSendReceiveAck(cmd);
        }

        public void ReadFile()
        {
            if (!string.IsNullOrEmpty(SelectedFile))
            {
                var cmd = _protocol.CreateReadFile(SelectedFile);
                var enc = _protocolHelper.EncryptMessage(cmd);

                var ackEnc = Connection.SendReceive(enc);
                if (ackEnc == null)
                {
                    AddLog("No anwser");
                    return;
                }

                byte idxCheck = 0;
                List<byte> memory = new List<byte>();

                while (true)
                {
                    var ack = _protocolHelper.DecryptData(ackEnc);
                    var result = _protocol.Parse(ack, out _index, out _resultData);

                    if (result == ProtocolResult.Ack)
                    {
                        if (idxCheck != _index)
                        {
                            AddLog("Read File index error!");
                            break;
                        }
                        idxCheck++;
                        memory.AddRange(_resultData);
                        cmd = _protocol.CreateAck();
                        enc = _protocolHelper.EncryptMessage(cmd);
                        ackEnc = Connection.SendReceive(enc);
                        if (ackEnc == null)
                        {
                            AddLog("No anwser");
                            break;
                        }
                    }
                    else if (result == ProtocolResult.FileEnd)
                    {
                        cmd = _protocol.CreateAck();
                        enc = _protocolHelper.EncryptMessage(cmd);
                        Connection.Send(enc);

                        var sfd = new SaveFileDialog();
                        sfd.FileName = SelectedFile;
                        if (sfd.ShowDialog() == true)
                        {
                            File.WriteAllBytes(sfd.FileName, memory.ToArray());
                        }
                        break;
                    }
                    else
                    {
                        AddLog("Read File failed -> " + result);
                        break;
                    }
                }
            }
        }

        public void SendFile()
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                var filename = Path.GetFileName(ofd.FileName);
                if (Path.GetExtension(filename) == ".png")
                {
                    var img = new Bitmap(ofd.FileName);
                    filename = $"{Path.GetFileNameWithoutExtension(filename)}_{img.Width}_{img.Height}.png";
                }
                var cmd = _protocol.CreateWriteFile(filename);
                var res = EncryptSendReceiveAck(cmd);
                if (res == ProtocolResult.AckAck)
                {
                    using (var stream = File.OpenRead(ofd.FileName))
                    {
                        var length = stream.Length;
                        byte index = 0;
                        var success = true;
                        while (length > 0)
                        {
                            var buffer = new byte[length > Protocol.FILE_BUFFER_SIZE ? Protocol.FILE_BUFFER_SIZE : length];
                            stream.Read(buffer, 0, buffer.Length);
                            cmd = _protocol.CreateFileTransferCommand(index, buffer);
                            res = EncryptSendReceiveAck(cmd);
                            if (res == ProtocolResult.AckAck)
                            {
                                length -= buffer.Length;
                                index++;
                            }
                            else
                            {
                                AddLog("Send File failed -> " + res);
                                success = false;
                                break;
                            }
                        }
                        if (success)
                        {
                            var end = _protocol.CreateFileTransferEndCommand();
                            EncryptSendReceiveAck(end);
                        }
                    }
                }

            }
        }

        public void SetTime()
        {
            var cmd = _protocol.CreateTimeCommand();
            EncryptSendReceiveAck(cmd);
        }

        public void SendReboot()
        {
            var cmd = _protocol.CreateRebootCommand();
            EncryptSendReceice(cmd);
        }

        public void ReadTime()
        {
            var cmd = _protocol.CreateTimeRequestCommand();
            EncryptSendReceice(cmd);
        }

        public void ReadInputs()
        {
            var cmd = _protocol.CreateInputRequestCommand();
            EncryptSendReceice(cmd);
        }

        public void ReadTouchButtons()
        {
            var cmd = _protocol.CreateTouchButtonRequestCommand();
            EncryptSendReceice(cmd);
        }

        public void ReadFileList()
        {
            var cmd = _protocol.CreateRequestFileListCommand();
            EncryptSendReceice(cmd);
        }

        public void DeleteFile()
        {
            if (!string.IsNullOrEmpty(SelectedFile))
            {
                var cmd = _protocol.CreateDeleteFileCommand(SelectedFile);
                if (EncryptSendReceiveAck(cmd) == ProtocolResult.Ack)
                {
                    TerminaFiles.Remove(SelectedFile);
                    SelectedFile = null;
                }
            }
        }

        public void ClearTouchId()
        {
            TouchButtonId = "-";
        }

        public void ReadSystemInfo()
        {
            var cmd = _protocol.CreateVersionRequestCommand();
            EncryptSendReceice(cmd);
        }

        private void OnInputChanged(object o, bool[] inputStates)
        {
            if (inputStates.Length != 4)
            { AddLog("Input states length incorrect!"); }

            InputState1 = inputStates[0];
            InputState2 = inputStates[1];
            InputState3 = inputStates[2];
            InputState4 = inputStates[3];
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



        // ********* Automatic Test *******
       
        public void Auto_StartIOTest()
        {
            _AutoIOTest.StartIOTest();
        }

        public void Auto_StopIOTest()
        {
            _AutoIOTest.StopIOTest();
        }
        public IEnumerable<string> AutoTest_ComPorts => SerialPort.GetPortNames();

        public string AutoTest_ComPortReader1 { get; set; }

        private string _Auto_BarcodeToWrite;
        public string Auto_BarcodeToWrite
        {
            get { return _Auto_BarcodeToWrite; }
            set
            {
                if (value == _Auto_BarcodeToWrite) return;
                _Auto_BarcodeToWrite = value;
                NotifyOfPropertyChange(nameof(Auto_BarcodeToWrite));
            }
        }

        public void Auto_StartStopWriteToReader1()
        {
            if (string.IsNullOrEmpty(AutoTest_ComPortReader1))
            {
                AddLog("No com-port selected");
                return;
            }

            if (_AutoReaderTest.IsConnected)
            {
                _AutoReaderTest.StopTest();
                Auto_StartStopWriteToReader1Content = "Start";
            }
            else
            {
                var result = _AutoReaderTest.StartTest(AutoTest_ComPortReader1);
                if (result)
                {
                    Auto_StartStopWriteToReader1Content = "Stop";
                }
            }
        }

        private string _Auto_StartStopWriteToReader1Content = "Start";
        public string Auto_StartStopWriteToReader1Content
        {
            get { return _Auto_StartStopWriteToReader1Content; }
            set
            {
                if (value == _Auto_StartStopWriteToReader1Content) return;
                _Auto_StartStopWriteToReader1Content = value;
                NotifyOfPropertyChange(nameof(Auto_StartStopWriteToReader1Content));
            }
        }


        // ***** AutoComplete Test *****
        public void Auto_StartCompleteTest()
        {
            if (string.IsNullOrEmpty(AutoTest_ComPortReader1))
            {
                AddLog("No com-port selected");
                return;
            }

            if (_AutoCompleteTest.IsConnected)
            {
                _AutoCompleteTest.StopTest();
                Auto_StartStopCompleteTestContent = "Start";
            }
            else
            {
                var result = _AutoCompleteTest.StartTest(AutoTest_ComPortReader1, AutoTest_UsePhysicalMedia);
                if (result)
                {
                    Auto_StartStopCompleteTestContent = "Stop";
                }
            }
        }

        private string _Auto_StartStopCompleteTestContent = "Start";
        public string Auto_StartStopCompleteTestContent
        {
            get { return _Auto_StartStopCompleteTestContent; }
            set
            {
                if (value == _Auto_StartStopCompleteTestContent) return;
                _Auto_StartStopCompleteTestContent = value;
                NotifyOfPropertyChange(nameof(Auto_StartStopCompleteTestContent));
            }
        }

        private bool _AutoTest_UsePhysicalMedia;

        public bool AutoTest_UsePhysicalMedia
        {
            get { return _AutoTest_UsePhysicalMedia; }
            set
            {
                if (value == _AutoTest_UsePhysicalMedia) return;
                _AutoTest_UsePhysicalMedia = value;
                NotifyOfPropertyChange(nameof(AutoTest_UsePhysicalMedia));
            }
        }

        public void SendBroadcast()
        {
            var udpConnection = _Connection as UdpConnection;
            if (udpConnection != null)
            {
                udpConnection.SendBroadcast();
            }
            else
            {
                LogError("UDP-Connection is closed!");
            }
        }
    }
}