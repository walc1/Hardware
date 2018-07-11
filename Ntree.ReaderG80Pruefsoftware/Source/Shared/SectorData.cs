using System;
using System.Text;

namespace Shared
{
    public enum SectorState : byte
    {
        Ok = 0x00,
        NoTransponder = 0x01,
        DataFalse = 0x02,
        WriteError = 0x03,
        AddressError = 0x04,
        WrongTransponderType = 0x05,
        AuthentError = 0x08,
        GeneralError = 0x0E,
        RFCommError = 0x83,
        DataBufferOverflow = 0x93,
        MoreData = 0x94,
        ISO155693Error = 0x95,
        ISO14443Error = 0x96,
        NoISO14443ATransponder = 0x70,
        UnknownError = 0x99,

    }

    public class SectorData
    {
        public byte State { get; set; }
        public byte Type { get; set; }
        public byte SectorNr { get; set; }
        public byte[] Data { get; set; }
    }
}
