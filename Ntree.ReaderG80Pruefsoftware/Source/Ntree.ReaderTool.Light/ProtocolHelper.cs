using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using Server;
using Shared;

namespace TestServer
{
    public class ProtocolHelper
    {
        private readonly Xtea _xtea;

        public ProtocolHelper(byte[] xteaKey)
        {
            _xtea = new Xtea(xteaKey);
        }

        public byte[] EncryptMessage(byte[] data)
        {
            var temp = data.ToList();
            var rest = data.Length % 8;
            for (int i = 0; i < 8 - rest; i++)
            {
                temp.Add(0);
            }
            var encrypt = _xtea.Encrypt(temp.ToArray());
            var hex = BytesToHexString(encrypt);
            var crc = Crc16.ComputeChecksumBytes(hex);
            var all = new List<byte>();
            all.AddRange(hex);
            all.AddRange(BytesToHexString(crc));
            all.Add(Protocol.END);
            return  all.ToArray();
        }

        public byte[] DecryptData(byte[] rawdata)
        {
            if (rawdata == null)
            {
                throw new ArgumentException("Data must not be null");
            }
            var crcCalced = Crc16.ComputeChecksum(rawdata.Take(rawdata.Length - 5).ToArray());

            var msg = Encoding.UTF8.GetString(rawdata).TrimEnd((char)Protocol.END);

            var data = HexStringToBytes(msg);
            var crc = data[data.Length - 2] + (data[data.Length - 1] << 8);
            if (crc != crcCalced)
            {
                throw new Exception("CRC error");
            }
            return _xtea.Decrypt(data.Take(data.Length - 2).ToArray());
        }

        private byte[] HexStringToBytes(string hex)
        {
            hex = hex.TrimStart('\0');
            if (string.IsNullOrEmpty(hex) || hex.Length % 2 != 0)
            {
                throw new Exception("Hex string length must be a multiple of two.");
            }

            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = HexToByte(hex.Substring(i * 2, 2));
            }

            return bytes;
        }

        private byte HexToByte(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length != 2)
            {
                throw new Exception("Only two hex digits can be converted to a byte.");
            }
            return byte.Parse(hex, NumberStyles.AllowHexSpecifier);
        }

        private byte[] BytesToHexString(byte[] bytes)
        {
            return Encoding.UTF8.GetBytes(bytes.Aggregate(string.Empty, (curHex, b) => curHex + $"{b:X2}"));
        }
    }
}