using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TestServer.ViewModels;

namespace TestServer.AutoTest
{
    public class AutoIOTest
    {
        private ProtocolManager _ProtocolManager;
        private Protocol _protocol;
        private MainViewModel _ViewModel;
        private bool _CancelIOTest = false;

        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();

        public AutoIOTest(Protocol argProtocol, ProtocolManager argProtocolManager, MainViewModel argViewModel)
        {
            _protocol = argProtocol;
            _ProtocolManager = argProtocolManager;
            _ViewModel = argViewModel;

            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            dispatcherTimer.Stop();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            if (_CancelIOTest)
            {
                return;
            }

            RunIOTest();

            if (_CancelIOTest)
            {
                return;
            }
            dispatcherTimer.Start();
        }

        public void StartIOTest()
        {
            RunIOTest();
            //_CancelIOTest = false;
            //dispatcherTimer.Start();
        }

        public void StopIOTest()
        {
            _CancelIOTest = true;
        }

        private bool RunIOTest()
        {
            Log("Start IO Test");

            byte ouputMask = 0;

            // reset output 1-3
            //SetOutputAndCheckInput(ref ouputMask, 1, false);
            //SetOutputAndCheckInput(ref ouputMask, 2, false);
            //SetOutputAndCheckInput(ref ouputMask, 3, false);

            // set-reset output 1
            SetOutputAndCheckInput(ref ouputMask, 1, true);
            SetOutputAndCheckInput(ref ouputMask, 1, false);

            // set-reset output 2
            SetOutputAndCheckInput(ref ouputMask, 2, true);
            SetOutputAndCheckInput(ref ouputMask, 2, false);

            // set-reset output 3
            SetOutputAndCheckInput(ref ouputMask, 3, true);
            SetOutputAndCheckInput(ref ouputMask, 3, false);

            // set output 1-3
            SetOutputAndCheckInput(ref ouputMask, 1, true);
            SetOutputAndCheckInput(ref ouputMask, 2, true);
            SetOutputAndCheckInput(ref ouputMask, 3, true);

            // reset output 1-3
            SetOutputAndCheckInput(ref ouputMask, 1, false);
            SetOutputAndCheckInput(ref ouputMask, 2, false);
            SetOutputAndCheckInput(ref ouputMask, 3, false);

            return true;
        }

        private bool SetOutputAndCheckInput(ref byte argMask, byte argRelaisNr, bool argValue)
        {
            // set output
            Log($"Set output {argRelaisNr} to {argValue}.");
            var result = SendRelais(ref argMask, argRelaisNr, argValue);
            Log($"Result: {result}");

            // wait for input
            Log($"Wait for input {argRelaisNr}");
            var counter = 0;
            var maxCycle = 100;

            switch (argRelaisNr)
            {
                case 1:
                    while (_ViewModel.InputState1 != argValue)
                    {
                        if (counter >= maxCycle)
                        {
                            Log("Error - Timeout elapsed");
                            return false;
                        }
                        counter++;
                        Thread.Sleep(10);
                    }
                    break;
                case 2:
                    while (_ViewModel.InputState2 != argValue)
                    {
                        if (counter >= maxCycle)
                        {
                            Log("Error - Timeout elapsed");
                            return false;
                        }
                        counter++;
                        Thread.Sleep(10);
                    }
                    break;
                case 3:
                    while (_ViewModel.InputState3 != argValue)
                    {
                        if (counter >= maxCycle)
                        {
                            Log("Error - Timeout elapsed");
                            return false;
                        }
                        counter++;
                        Thread.Sleep(10);
                    }
                    break;
                case 4:
                    while (_ViewModel.InputState4 != argValue)
                    {
                        if (counter >= maxCycle)
                        {
                            Log("Error - Timeout elapsed");
                            return false;
                        }
                        counter++;
                        Thread.Sleep(10);
                    }
                    break;
                default:
                    Log($"Error - SetOutputAndCheckInput for {argRelaisNr} not supported.");
                    return false;
            }

            Log($"Input was set after {counter} ms.");

            return true;
        }

        private ProtocolResult SendRelais(ref byte argMask, byte argRelaisNr, bool argValue)
        {
            // allow only Relais 1-4
            if (argRelaisNr < 1 || argRelaisNr > 4)
            {
                Log($"SendRelais invalid nr {argRelaisNr}");
                return ProtocolResult.UnknownError;
            }

            int tempMask = argMask;

            if (argValue)
            {
                // set bit
                tempMask |= 1 << (argRelaisNr - 1);
            }
            else
            {
                // reset bit
                tempMask &= ~(1 << (argRelaisNr - 1));
            }

            argMask = Convert.ToByte(tempMask);

            var cmd = _protocol.CreateRelaisCommand(argMask);
            return _ProtocolManager.EncryptSendReceiveAck(cmd);
        }

        private void Log(string argMessage)
        {
            _ViewModel.AddLog(argMessage);
        }
    }
}
