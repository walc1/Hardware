namespace Shared
{
    public class ColorInfo
    {
        public static ColorInfo Black = new ColorInfo(0, 0, 0);
        public static ColorInfo White = new ColorInfo(255, 255, 255);

        public ColorInfo()
        {
        }

        public ColorInfo(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
    }
}