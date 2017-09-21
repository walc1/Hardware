namespace Shared
{
    public class LineInfo
    {
        public LineInfo(byte index, ColorInfo color, ushort x1, ushort y1, ushort x2, ushort y2, byte width)
        {
            Index = index;
            Color = color;
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
            Width = width;
        }

        public byte Index { get; set; }
        public ColorInfo Color { get; set; }
        public ushort X1 { get; set; }
        public ushort Y1 { get; set; }
        public ushort X2 { get; set; }
        public ushort Y2 { get; set; }
        public byte Width { get; set; }
    }
}