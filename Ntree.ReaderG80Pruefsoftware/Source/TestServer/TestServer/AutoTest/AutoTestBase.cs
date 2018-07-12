using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TestServer.ViewModels;

namespace TestServer.AutoTest
{
    public class AutoTestBase
    {
        protected ProtocolManager _ProtocolManager;
        protected Protocol _protocol;
        protected MainViewModel _ViewModel;


        public AutoTestBase(Protocol argProtocol, ProtocolManager argProtocolManager, MainViewModel argViewModel)
        {
            _protocol = argProtocol;
            _ProtocolManager = argProtocolManager;
            _ViewModel = argViewModel;
        }


        protected ProtocolResult SendRelais(ref byte argMask, byte argRelaisNr, bool argValue)
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

        protected void Log(string argMessage)
        {
            _ViewModel.AddLog(argMessage);
        }

        protected void LogError(string argMessage)
        {
            _ViewModel.AddErrorLog(argMessage);
        }
    }
}
