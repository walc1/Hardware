using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestServer.ViewModels;

namespace TestServer
{
    public class ProtocolManager
    {
        private ProtocolHelper _protocolHelper;
        private Protocol _protocol;
        private byte _index;
        private byte[] _resultData;
        private MainViewModel _ViewModel;

        public ProtocolManager(ProtocolHelper argProtocolHelper, Protocol argProtocol, MainViewModel argViewModel)
        {
            _protocolHelper = argProtocolHelper;
            _protocol = argProtocol;
            _ViewModel = argViewModel;
        }

        public IConnection Connection { get; set; }

        public void EncryptSendReceice(byte[] data)
        {
            // Note: Bestätigt result mit "ACK"
            var enc = _protocolHelper.EncryptMessage(data);
            var answer = Connection.SendReceive(enc);
            if (answer != null)
            {
                var ack = _protocolHelper.DecryptData(answer);
                var parseRes = _protocol.Parse(ack, out _index, out _resultData);
                if (parseRes != ProtocolResult.Ack && parseRes != ProtocolResult.None)
                {
                    AddLog("SendReceive failed: " + parseRes);
                    var nack = _protocol.CreateNack(parseRes);
                    Connection.Send(_protocolHelper.EncryptMessage(nack));
                }
                else
                {
                    var cmd = _protocol.CreateAck();
                    Connection.Send(_protocolHelper.EncryptMessage(cmd));
                }
            }
            else
            {
                AddLog("No answer");
            }
        }

        public ProtocolResult EncryptSendReceiveAck(byte[] msg)
        {
            // Note: Keine bestätigung vom result
            var enc = _protocolHelper.EncryptMessage(msg);
            var ackEnc = Connection.SendReceive(enc);
            if (ackEnc == null)
            {
                AddLog("No answer");
                return ProtocolResult.Timeout;
            }
            try
            {
                var ack = _protocolHelper.DecryptData(ackEnc);
                var result = _protocol.Parse(ack, out _index, out _resultData);
                if (result != ProtocolResult.Ack && result != ProtocolResult.AckAck)
                {
                    AddLog("NAK: " + result);
                }
                return result;
            }
            catch (Exception e)
            {
                AddLog(e.ToString());
                return ProtocolResult.UnknownError;
            }
        }


        private void AddLog(string argMessage)
        {
            _ViewModel.AddLog(argMessage);
        }

    }
}
