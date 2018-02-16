using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.SerialPort.Properties;

namespace Test.SerialPort
{
    class Program
    {
        private static System.IO.Ports.SerialPort _serial;
        private static bool _isConnected;
        private static System.Timers.Timer _Timer;

        static void Main(string[] args)
        {
            _Timer = new System.Timers.Timer(200);
            _Timer.Elapsed += _Timer_Elapsed;
            _Timer.Start();

            Connect();
            Console.WriteLine("Write data...");
            Console.ReadLine();
        }

        private static void _Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_isConnected)
            {
                try
                {                    
                    _serial.Write(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 }, 0, 10);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static void Connect()
        {
            try
            {
                Console.WriteLine("Connecting...");
                _serial = new System.IO.Ports.SerialPort(Settings.Default.COM, Settings.Default.Baudrate); // 9600
                _serial.Open();
                _serial.DiscardInBuffer();
                _isConnected = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

    }
}
