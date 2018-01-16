using Shared;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using TestServer.ViewModels;

namespace TestServer.AutoTest
{
    public class AutoReaderTest : AutoTestBase
    {
        private ulong _MediaToWrite = 0;

        private Thread _AutoTaskWriteMediacode;
        private CancellationTokenSource _CancellationToken;

        private SerialPort _serial;
        private bool _isConnected;
        private UInt64 _LastReaderMediaCode1 = 0;
        private UInt64 _LastReaderMediaCode2 = 0;
        private UInt64 _LastReaderMediaCode3 = 0;

        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                _isConnected = value;
            }
        }
        public AutoReaderTest(Protocol argProtocol, ProtocolManager argProtocolManager, MainViewModel argViewModel)
            : base(argProtocol, argProtocolManager, argViewModel)
        {

        }

        public bool StartTest(string argComPort)
        {
            try
            {
                _CancellationToken = new CancellationTokenSource();
                _MediaToWrite = 0;
                _LastReaderMediaCode1 = 0;
                _LastReaderMediaCode2 = 0;
                _LastReaderMediaCode3 = 0;

                if (!IsConnected)
                {
                    _serial = new SerialPort(argComPort, 19200); // 9600
                    _serial.Open();
                    _serial.DiscardInBuffer();
                    //_serial.DataReceived += SerialOnDataReceived;
                    IsConnected = true;
                }

                _AutoTaskWriteMediacode = new Thread(() =>
                {
                    ExecuteWriteMediacode();
                });
                _AutoTaskWriteMediacode.Start();
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
            _AutoTaskWriteMediacode.Join(1000);
        }
                
        public void CheckReaderMedia1(string argMediaCode)
        {
            if (!IsConnected)
            {
                return;
            }

            UInt64 testResult = 0;
            if (!UInt64.TryParse(argMediaCode, out testResult))
            {
                LogError($"CheckReaderMedia1 TryParse failed for {argMediaCode}.");
            }

            // Note: Das Termination Zeichen wurde bereits entfernt.
            if (argMediaCode.Length != 24)
            {
                LogError($"CheckReaderMedia1 failed. Length {argMediaCode.Length}. Expected {24}.");
            }

            if (testResult != _LastReaderMediaCode1 + 1)
            {
                LogError($"CheckReaderMedia1 failed for {argMediaCode}. Expected {_LastReaderMediaCode1 + 1}.");
            }

            _LastReaderMediaCode1 = testResult;
        }

        public void CheckReaderMedia2(string argMediaCode)
        {
            if (!IsConnected)
            {
                return;
            }

            UInt64 testResult;
            if (!UInt64.TryParse(argMediaCode, out testResult))
            {
                LogError($"CheckReaderMedia2 TryParse failed for {argMediaCode}.");
            }

            if (argMediaCode.Length != 24)
            {
                LogError($"CheckReaderMedia2 failed. Length {argMediaCode.Length}. Expected {24}.");
            }

            if (testResult != _LastReaderMediaCode2 + 1)
            {
                LogError($"CheckReaderMedia2 failed for {argMediaCode}. Expected {_LastReaderMediaCode2 + 1}.");
            }

            _LastReaderMediaCode2 = testResult;
        }

        private void ExecuteWriteMediacode()
        {
            while (!_CancellationToken.IsCancellationRequested && IsConnected)
            {
                var data = GetNextMediacode();
                if (_serial.IsOpen)
                {
                    _ViewModel.Auto_BarcodeToWrite = Encoding.UTF8.GetString(data).ToString().Trim('\r');
                    _serial.Write(data, 0, data.Length);
                }

                Thread.Sleep(200);
            }
        }
        
        private byte[] GetNextMediacode()
        {
            _MediaToWrite++;
            var mediacodeAsString = _MediaToWrite.ToString().PadLeft(24, '0');

            //_ViewModel.Auto_BarcodeToWrite = mediacodeAsString;

            byte[] toBytes = Encoding.Default.GetBytes(mediacodeAsString);

            byte[] resultBytes = new byte[toBytes.Length + 1];
            Array.Copy(toBytes, resultBytes, toBytes.Length);
            // set termination
            resultBytes[resultBytes.Length - 1] = 13;

            return resultBytes;
        }

    }
}
