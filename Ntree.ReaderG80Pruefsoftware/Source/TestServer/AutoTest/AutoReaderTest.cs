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
    public class AutoReaderTest
    {
        private ProtocolManager _ProtocolManager;
        private Protocol _protocol;
        private MainViewModel _ViewModel;
        private long _barcodeToWrite = 0;

        private List<TestInstruction> _InstructionList = new List<TestInstruction>();
        private int _InstructionIdx = 0;

        private Thread _AutoTaskWriteBarcode;
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

        public AutoReaderTest(Protocol argProtocol, ProtocolManager argProtocolManager, MainViewModel argViewModel)
        {
            _protocol = argProtocol;
            _ProtocolManager = argProtocolManager;
            _ViewModel = argViewModel;

            InitTestList();
        }

        public void StartTest(string argComPort)
        {
            _CancellationToken = new CancellationTokenSource();
            _InstructionIdx = 0;
            _barcodeToWrite = 0;

            if (!IsConnected)
            {
                _serial = new SerialPort(argComPort, 19200); // 9600
                _serial.Open();
                _serial.DiscardInBuffer();
                //_serial.DataReceived += SerialOnDataReceived;
                IsConnected = true;
            }

            _AutoTaskWriteBarcode = new Thread(() =>
            {
                ExecuteWriteBarcode();
            });
            _AutoTaskWriteBarcode.Start();                      
        }

        public void StopTest()
        {
            _serial.Close();
            IsConnected = false;

            _CancellationToken.Cancel();
            _AutoTaskWriteBarcode.Join(1000);
        }

        private void InitTestList()
        {
            // write Barcode 
            //CreateTestInstruction(InstructionType.WriteBarcode);
        }

        private void ExecuteWriteBarcode()
        {
            while (!_CancellationToken.IsCancellationRequested)
            {
                var data = GetNextBarcode();
                _serial.Write(data);

                Thread.Sleep(1000);
            }
        }
        
        private string GetNextBarcode()
        {
            _barcodeToWrite++;
            var barcodeAsString = _barcodeToWrite.ToString().PadLeft(24, '0');
            _ViewModel.Auto_BarcodeToWrite = barcodeAsString;
            return barcodeAsString;
        }

        private void ExecuteNextInstruction(int argInstructionIdx)
        {
            if (_CancellationToken.IsCancellationRequested)
            {
                return;
            }
        }

        private void ExecuteTimeDelay(TestInstruction argInstruction)
        {
            Thread.Sleep(argInstruction.TimeDelayMs);
            ExecuteNextStep();
        }

        private void ExecuteNextStep()
        {
            Thread.Sleep(100);
            _InstructionIdx++;
            if (_InstructionIdx > _InstructionList.Count - 1)
            {
                _InstructionIdx = 0;
            }

            ExecuteNextInstruction(_InstructionIdx);
        }

        private void Log(string argMessage)
        {
            _ViewModel.AddLog(argMessage);
        }

        private void LogError(string argMessage)
        {
            _ViewModel.AddErrorLog(argMessage);
        }
    }
}
