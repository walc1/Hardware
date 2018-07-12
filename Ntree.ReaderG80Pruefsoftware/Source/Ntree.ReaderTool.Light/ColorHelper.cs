using System.Windows.Media;
using Shared;

namespace TestServer
{
    public class ColorHelper
    {
        public static ColorInfo Get(Color color)
        {
            return new ColorInfo { R = color.R, G = color.G, B = color.B };
        }
    }
}