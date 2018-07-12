using Shared;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Media;
using TestServer.ViewModels;

namespace TestServer.AutoTest
{
    enum AutoCompleteTestState
    {
        WriteMsgKarteBitte,
        WriteMediacode, 
        CheckMediacode, 
        WriteOutput,
        WriteMsgEingangFrei,
        WaitInputForTrue,
        ResetOutput,
        WaitInputForFalse
    }

    public class AutoCompleteTest : AutoTestBase
    {
        private ulong _MediaToWrite = 0;
        private AutoCompleteTestState _State;
        private string _LastSentMediacode = "";
        private string _LastReadMediacode = "";
        private byte _OuputMask = 0;
        private Color _TextColorWhite { get; set; } = Colors.White;
        private Color _TextColorGreen { get; set; } = Colors.Green;
        private bool _UsePhysicalMedia;

        private Thread _WorkerTask;
        private CancellationTokenSource _CancellationToken;

        private SerialPort _serial;
        private bool _isConnected;


        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                _isConnected = value;
            }
        }

        public AutoCompleteTest(Protocol argProtocol, ProtocolManager argProtocolManager, MainViewModel argViewModel)
            : base(argProtocol, argProtocolManager, argViewModel)
        {

        }

        public bool StartTest(string argComPort, bool argUsePhysicalMedia)
        {
            // test
            // \a��\u0001\0\u001cV
            // Length: 7
            //var stringRequest =  Encoding.UTF8.GetString(new byte[] { 7, 255, 176, 1, 0, 28, 86 });

            _UsePhysicalMedia = argUsePhysicalMedia;

            try
            {
                _CancellationToken = new CancellationTokenSource();
                _MediaToWrite = 0;
                _LastSentMediacode = string.Empty;
                _LastReadMediacode = string.Empty;

                if (!IsConnected)
                {
                    _serial = new SerialPort(argComPort, 19200); // 9600
                    _serial.Open();
                    _serial.DiscardInBuffer();
                    IsConnected = true;
                }

                _WorkerTask = new Thread(() =>
                {
                    while (!_CancellationToken.IsCancellationRequested)
                    {
                        RunWorkerTask();
                        Thread.Sleep(50);

                        //var cmd = _protocol.CreatePortRedirectCommand(3, 5000, 6, new byte[] { 7, 255, 176, 1, 0, 28, 86 });
                        //_ProtocolManager.EncryptSendReceice(cmd);
                    }
                });
                _WorkerTask.Start();
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
            return false;
        }

        public void StopTest()
        {
            _serial.Close();
            IsConnected = false;

            _CancellationToken.Cancel();
            _WorkerTask.Join(1000);
        }

        private int _WaitCounter = 0;
        private byte[] _Cmd;
        private ProtocolResult _Result;

        private void RunWorkerTask()
        {
            switch (_State)
            {
                case AutoCompleteTestState.WriteMsgKarteBitte:
                    // clear image
                    _Cmd = _protocol.CreateDisplayDeleteCommand(1, DisplayCommand.Image);
                    _Result = _ProtocolManager.EncryptSendReceiveAck(_Cmd);

                    // write text
                    _Cmd = _protocol.CreateDisplayTextCommand(2, new ColorInfo(_TextColorWhite.R, _TextColorWhite.G, _TextColorWhite.B), 140, 200, 33, "Ihre Karte bitte");
                    _Result = _ProtocolManager.EncryptSendReceiveAck(_Cmd);
                    if (_Result == ProtocolResult.Ack || _Result == ProtocolResult.AckAck)
                    {
                        _Cmd = _protocol.CreateDisplayInvalidateCommand();
                        _Result = _ProtocolManager.EncryptSendReceiveAck(_Cmd);
                        if (_Result == ProtocolResult.Ack || _Result == ProtocolResult.AckAck)
                        {
                            // ok
                            //Thread.Sleep(1000);
                            SetNextState(AutoCompleteTestState.WriteMediacode);
                        }
                        else
                        {
                            LogError($"WriteMsgKarteBitte failed: {_Result}");
                        }
                    }
                    else
                    {
                        LogError($"WriteMsgKarteBitte failed: {_Result}");
                    }
                    break;
                case AutoCompleteTestState.WriteMediacode:
                    if (_UsePhysicalMedia)
                    {
                        SetNextState(AutoCompleteTestState.CheckMediacode);
                    }
                    else
                    {
                        var data = GetNextMediacode();
                        if (_serial.IsOpen)
                        {
                            _ViewModel.Auto_BarcodeToWrite = Encoding.UTF8.GetString(data).ToString().Trim('\r');
                            _LastSentMediacode = _ViewModel.Auto_BarcodeToWrite;
                            _serial.Write(data, 0, data.Length);
                            SetNextState(AutoCompleteTestState.CheckMediacode);
                        }
                        else
                        {
                            LogError("SerialPort is not open");
                        }
                    }                  
                    break;
                case AutoCompleteTestState.CheckMediacode:

                    if (_ViewModel.ReadMediaRaw == _LastReadMediacode)
                    {
                        if (_UsePhysicalMedia)
                        {
                            ; // just wait
                        }
                        else
                        {
                            // wait
                            _WaitCounter++;
                            if (_WaitCounter > 50)
                            {
                                LogError("CheckMediacode Timeout");
                                SetNextState(AutoCompleteTestState.WriteMediacode);
                            }
                        }
                    }
                    else
                    {
                        if (_UsePhysicalMedia)
                        {
                            if (_ViewModel.ReadMediaRaw.Length > 10)
                            {
                                SetNextState(AutoCompleteTestState.WriteOutput);
                            }
                        }
                        else
                        {
                            // check
                            if (_ViewModel.ReadMediaRaw == _LastSentMediacode)
                            {
                                SetNextState(AutoCompleteTestState.WriteOutput);
                            }
                            else
                            {
                                LogError("Mediacode not match, try again");
                                SetNextState(AutoCompleteTestState.WriteMediacode);
                            }
                        }
                    }
                    break;
                case AutoCompleteTestState.WriteOutput:
                    _Result = base.SendRelais(ref _OuputMask, 1, true);
                    if (_Result == ProtocolResult.Ack || _Result == ProtocolResult.AckAck)
                    {
                        // clear text
                        _Cmd = _protocol.CreateDisplayTextCommand(2, new ColorInfo(_TextColorGreen.R, _TextColorGreen.G, _TextColorGreen.B), 180, 200, 33, "");
                        _Result = _ProtocolManager.EncryptSendReceiveAck(_Cmd);

                        // image
                        _Cmd = _protocol.CreateDisplayImageCommand(1, 280, 160, "check_200_191.png");
                        _Result = _ProtocolManager.EncryptSendReceiveAck(_Cmd);

                        // text
                        //_Cmd = _protocol.CreateDisplayTextCommand(2, new ColorInfo(_TextColorGreen.R, _TextColorGreen.G, _TextColorGreen.B), 180, 200, 33, "Eingang Frei");
                        //_Result = _ProtocolManager.EncryptSendReceiveAck(_Cmd);

                        if (_Result == ProtocolResult.Ack || _Result == ProtocolResult.AckAck)
                        {
                            _Cmd = _protocol.CreateDisplayInvalidateCommand();
                            _Result = _ProtocolManager.EncryptSendReceiveAck(_Cmd);
                            if (_Result == ProtocolResult.Ack || _Result == ProtocolResult.AckAck)
                            {
                                // ok
                                Thread.Sleep(1000);
                                SetNextState(AutoCompleteTestState.WaitInputForTrue);
                            }
                            else
                            {
                                LogError($"WriteMsgEingangFrei failed: {_Result}");
                            }
                        }
                        else
                        {
                            LogError($"WriteMsgEingangFrei failed: {_Result}");
                        }
                    }
                    else
                    {
                        LogError($"WriteOutput failed: {_Result}");
                    }
                    break;
                case AutoCompleteTestState.WriteMsgEingangFrei:
                    //var cmdWriteEingangFrei = _protocol.CreateDisplayTextCommand(2, new ColorInfo(_TextColorGreen.R, _TextColorGreen.G, _TextColorGreen.B), 180, 200, 33, "Eingang Frei");
                    //var resultMsgFrei = _ProtocolManager.EncryptSendReceiveAck(cmdWriteEingangFrei);                    
                    //if (resultMsgFrei == ProtocolResult.Ack || resultMsgFrei == ProtocolResult.AckAck)
                    //{
                    //    // ok
                    //    Thread.Sleep(1000);
                    //    SetNextState(AutoCompleteTestState.WaitInputForTrue);
                    //}
                    //else
                    //{
                    //    LogError($"WriteMsgEingangFrei failed: {resultMsgFrei}");
                    //}
                    break;
                case AutoCompleteTestState.WaitInputForTrue:
                    if (_ViewModel.InputState1)
                    {
                        // ok
                        SetNextState(AutoCompleteTestState.ResetOutput);
                    }
                    else
                    {
                        // wait
                        _WaitCounter++;
                        if (_WaitCounter > 50)
                        {
                            LogError("WaitInputForTrue Timeout");
                            SetNextState(AutoCompleteTestState.WriteOutput);
                        }
                    }
                    break;
                case AutoCompleteTestState.ResetOutput:
                    var resultReset = base.SendRelais(ref _OuputMask, 1, false);
                    if (resultReset == ProtocolResult.Ack || resultReset == ProtocolResult.AckAck)
                    {
                        // ok
                        SetNextState(AutoCompleteTestState.WaitInputForFalse);
                    }
                    else
                    {
                        LogError($"WriteOutput failed: {resultReset}");
                    }
                    break;
                case AutoCompleteTestState.WaitInputForFalse:
                    if (_ViewModel.InputState1 == false)
                    {
                        if (_UsePhysicalMedia)
                        {
                            _LastReadMediacode = "";
                            _ViewModel.ReadMediaRaw = "";
                        }

                        // finish
                        Log($"Test finish");                        
                        SetNextState(AutoCompleteTestState.WriteMsgKarteBitte);
                    }
                    else
                    {
                        // wait
                        _WaitCounter++;
                        if (_WaitCounter > 50)
                        {
                            LogError("WaitInputForFalse Timeout");
                            SetNextState(AutoCompleteTestState.ResetOutput);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private void SetNextState(AutoCompleteTestState argNextState)
        {
            _State = argNextState;
            _WaitCounter = 0;
        }
        
        private byte[] GetNextMediacode()
        {
            _MediaToWrite++;
            var mediacodeAsString = _MediaToWrite.ToString().PadLeft(24, '0');

            byte[] toBytes = Encoding.Default.GetBytes(mediacodeAsString);

            byte[] resultBytes = new byte[toBytes.Length + 1];
            Array.Copy(toBytes, resultBytes, toBytes.Length);
            // set termination
            resultBytes[resultBytes.Length - 1] = 13;

            return resultBytes;
        }
    }
}
