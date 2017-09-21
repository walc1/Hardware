namespace Shared
{
    public class DisplayTextInfo
    {
        public DisplayTextInfo()
        {
            
        }

        public DisplayTextInfo(byte index, byte size, ColorInfo color, ushort x, ushort y, string text)
        {
            Index = index;
            Size = size;
            Color = color;
            X = x;
            Y = y;
            Text = text;
        }

        public byte Index { get; set; }
        public byte Size { get; set; }
        public ColorInfo Color { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public string Text { get; set; }
    }
}