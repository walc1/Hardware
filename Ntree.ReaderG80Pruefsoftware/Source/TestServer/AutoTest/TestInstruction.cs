using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestServer.AutoTest
{
    public enum InstructionType
    {
        SetRelais,
        ReadInput,
        TimeDelay, 
        WriteBarcode,
        ReadBarcode
    }

    public class TestInstruction
    {
        public InstructionType Command { get; set; }

        public bool Value { get; set; }

        public byte Index { get; set; }

        public int TimeDelayMs { get; set; }

        public string Barcode { get; set; }

    }
}
