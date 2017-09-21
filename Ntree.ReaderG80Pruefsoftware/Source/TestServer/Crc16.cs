namespace Server
{
    public static class Crc16
    {
        const ushort Poly = 4129;
        static readonly ushort[] Table = new ushort[256];
        private static readonly ushort InitialValue;

        public static ushort ComputeChecksum(byte[] bytes)
        {
            ushort crc = InitialValue;
            for (int i = 0; i < bytes.Length; i++)
            {
                crc = (ushort)((crc << 8) ^ Table[((crc >> 8) ^ (0xff & bytes[i]))]);
            }
            return crc;
        }

        public static byte[] ComputeChecksumBytes(byte[] bytes)
        {
            ushort crc = ComputeChecksum(bytes);
            return new byte[] { (byte)(crc >> 8), (byte)(crc & 0x00ff) };
        }

        static Crc16()
        {
            InitialValue = 4321;
            for (int i = 0; i < Table.Length; i++)
            {
                ushort temp = 0;
                var a = (ushort)(i << 8);
                for (int j = 0; j < 8; j++)
                {
                    if (((temp ^ a) & 0x8000) != 0)
                    {
                        temp = (ushort)((temp << 1) ^ Poly);
                    }
                    else
                    {
                        temp <<= 1;
                    }
                    a <<= 1;
                }
                Table[i] = temp;
            }
        }
    }
}