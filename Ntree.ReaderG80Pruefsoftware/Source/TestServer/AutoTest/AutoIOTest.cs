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

        private List<TestInstruction> _InstructionList = new List<TestInstruction>();
        private int _InstructionIdx = 0;
        private byte _OuputMask = 0;

        private Thread _AutoIOTestTask;
        private CancellationTokenSource _CancellationToken;

        public AutoIOTest(Protocol argProtocol, ProtocolManager argProtocolManager, MainViewModel argViewModel)
        {
            _protocol = argProtocol;
            _ProtocolManager = argProtocolManager;
            _ViewModel = argViewModel;

            InitTestList();
        }


        public void StartIOTest()
        {
            _CancellationToken = new CancellationTokenSource();
            _InstructionIdx = 0;

            _AutoIOTestTask = new Thread(() =>
            {
                ExecuteNextInstruction(_InstructionIdx);
            });
            _AutoIOTestTask.Start();                      
        }

        public void StopIOTest()
        {
            _CancellationToken.Cancel();
            _AutoIOTestTask.Join(1000);
        }

        private void InitTestList()
        {
            // reset output 
            CreateTestInstruction(InstructionType.SetRelais, 1, false);
            CreateTestInstruction(InstructionType.SetRelais, 2, false);
            CreateTestInstruction(InstructionType.SetRelais, 3, false);

            // wait 
            CreateTestInstruction(InstructionType.TimeDelay, 1000);

            // check input
            CreateTestInstruction(InstructionType.ReadInput, 1, false);
            CreateTestInstruction(InstructionType.ReadInput, 2, false);
            CreateTestInstruction(InstructionType.ReadInput, 3, false);

            // set output 1
            CreateTestInstruction(InstructionType.SetRelais, 1, true);

            // wait 
            CreateTestInstruction(InstructionType.TimeDelay, 1000);

            // check input
            CreateTestInstruction(InstructionType.ReadInput, 1, true);
            CreateTestInstruction(InstructionType.ReadInput, 2, false);
            CreateTestInstruction(InstructionType.ReadInput, 3, false);

            // set output 2
            CreateTestInstruction(InstructionType.SetRelais, 2, true);

            // wait 
            CreateTestInstruction(InstructionType.TimeDelay, 1000);

            // check input
            CreateTestInstruction(InstructionType.ReadInput, 1, true);
            CreateTestInstruction(InstructionType.ReadInput, 2, true);
            CreateTestInstruction(InstructionType.ReadInput, 3, false);

            // set output 3
            CreateTestInstruction(InstructionType.SetRelais, 3, true);

            // wait 
            CreateTestInstruction(InstructionType.TimeDelay, 1000);

            // check input
            CreateTestInstruction(InstructionType.ReadInput, 1, true);
            CreateTestInstruction(InstructionType.ReadInput, 2, true);
            CreateTestInstruction(InstructionType.ReadInput, 3, true);
        }

        private void ExecuteNextInstruction(int argInstructionIdx)
        {
            if (_CancellationToken.IsCancellationRequested)
            {
                return;
            }

            var instruction = _InstructionList[argInstructionIdx];
            switch (instruction.Command)
            {
                case InstructionType.SetRelais:
                    ExecuteSetRelais(instruction);
                    break;
                case InstructionType.ReadInput:
                    ExecuteReadInput(instruction);
                    break;
                case InstructionType.TimeDelay:
                    ExecuteTimeDelay(instruction);
                    break;
                default:
                    break;
            }

            if (instruction.Command != InstructionType.TimeDelay)
            {
                ExecuteNextStep();
            }
        }

        private void ExecuteSetRelais(TestInstruction argInstruction)
        {
            // set output
            Log($"Set output {argInstruction.Index} to {argInstruction.Value}.");
            var result = SendRelais(ref _OuputMask, argInstruction.Index, argInstruction.Value);

            if (result != ProtocolResult.Ack && result != ProtocolResult.AckAck)
            {
                LogError($"Result: {result}");
            }
            else
            {
                Log($"Result: {result}");
            }
        }

        private void ExecuteReadInput(TestInstruction argInstruction)
        {
            // read input
            Log($"Read input {argInstruction.Index}. Expected value {argInstruction.Value}.");

            switch (argInstruction.Index)
            {
                case 1:
                    if (_ViewModel.InputState1 != argInstruction.Value)
                    {
                        LogError($"Error - Input value {_ViewModel.InputState1}. Expected {argInstruction.Value}.");
                    }
                    break;
                case 2:
                    if (_ViewModel.InputState2 != argInstruction.Value)
                    {
                        LogError($"Error - Input value {_ViewModel.InputState2}. Expected {argInstruction.Value}.");
                    }
                    break;
                case 3:
                    if (_ViewModel.InputState3 != argInstruction.Value)
                    {
                        LogError($"Error - Input value {_ViewModel.InputState3}. Expected {argInstruction.Value}.");
                    }
                    break;
                case 4:
                    if (_ViewModel.InputState4 != argInstruction.Value)
                    {
                        LogError($"Error - Input value {_ViewModel.InputState4}. Expected {argInstruction.Value}.");
                    }
                    break;
                default:
                    LogError($"Error - ExecuteReadInput for index {argInstruction.Index} not supported.");
                    break;
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

        private void CreateTestInstruction(InstructionType argCmd, byte argIndex, bool argValue)
        {
            TestInstruction instruction = new TestInstruction();
            instruction.Command = argCmd;
            instruction.Index = argIndex;
            instruction.Value = argValue;

            _InstructionList.Add(instruction);
        }

        private void CreateTestInstruction(InstructionType argCmd, int argTimeDelayMS)
        {
            TestInstruction instruction = new TestInstruction();
            instruction.Command = argCmd;
            instruction.TimeDelayMs = argTimeDelayMS;

            _InstructionList.Add(instruction);
        }

        private ProtocolResult SendRelais(ref byte argMask, byte argRelaisNr, bool argValue)
        {
            // allow only Relais 1-4
            if (argRelaisNr < 1 || argRelaisNr > 4)
            {
                LogError($"SendRelais invalid nr {argRelaisNr}");
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

        private void LogError(string argMessage)
        {
            _ViewModel.AddErrorLog(argMessage);
        }
    }
}
