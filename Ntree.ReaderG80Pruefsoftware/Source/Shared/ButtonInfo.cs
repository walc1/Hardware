namespace Shared
{
    public class ButtonInfo
    {
        public ButtonInfo()
        {
        }

        public ButtonInfo(byte index, ColorInfo foregroundColor, ColorInfo buttonColor, byte fontSize, ushort x, ushort y, ushort width, ushort height, string text)
        {
            Index = index;
            ForegroundColor = foregroundColor;
            ButtonColor = buttonColor;
            FontSize = fontSize;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Text = text;
        }
        
        public byte Index { get; set; }
        public ColorInfo ForegroundColor { get; set; }
        public ColorInfo ButtonColor { get; set; }
        public byte FontSize { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public ushort Width { get; set; }
        public ushort Height { get; set; }
        public string Text { get; set; }
    }
}