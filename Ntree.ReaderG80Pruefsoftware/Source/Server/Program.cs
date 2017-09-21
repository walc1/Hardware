using System;
using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Timers;
using System.Windows.Media;
using Shared;

namespace Server
{
    class Server
    {
        private static string _recBuffer;
        private static NtreeProtocol _protocol;
        private static SerialPort _serial;
        private static Timer _displayResetTimer;

        private static byte _minutes = 5;


        static void Main(string[] args)
        {
            _serial = new SerialPort("COM8", 115200);
            _serial.Open();
            _serial.DiscardInBuffer();
            _protocol = new NtreeProtocol();
            _protocol.InputChanged += ProtocolOnInputChanged;
            _protocol.TouchButton += ProtocolOnTouchButton;

            _displayResetTimer = new Timer(5000);
         
            Console.ReadLine();
        }

        private static void ProtocolOnInputChanged(object sender, bool[] inputStates)
        {
            var text = string.Empty;
            for (int i = 0; i < inputStates.Length; i++)
            {
                text += $" Input{i + 1}: {inputStates[i]}\n";
            }
            Console.WriteLine($"Input changed:\n{text}");
        }

        private static void ProtocolOnTouchButton(object o, byte buttonId)
        {
            if (buttonId == 1 && _minutes > 0)
                _minutes--;
            if (buttonId == 2 && _minutes < 20)
                _minutes++;
        }

     
    }
}