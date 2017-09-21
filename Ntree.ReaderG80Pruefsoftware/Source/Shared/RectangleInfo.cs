namespace Shared
{
    public class RectangleInfo
    {
        public RectangleInfo()
        {
        }

        public RectangleInfo(byte index, ColorInfo color, ushort x, ushort y, ushort width, ushort height)
        {
            Index = index;
            Color = color;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
        
        public byte Index { get; set; }
        public ColorInfo Color { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public ushort Width { get; set; }
        public ushort Height { get; set; }
    }
}