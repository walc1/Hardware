namespace Shared
{
    public class ImageInfo
    {
        public ImageInfo(byte index, string filename, ushort x, ushort y)
        {
            Index = index;
            Filename = filename;
            X = x;
            Y = y;
        }

        public byte Index { get; set; }
        public string Filename { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
    }
}