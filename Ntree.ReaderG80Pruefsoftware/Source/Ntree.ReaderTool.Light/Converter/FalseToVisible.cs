using System.Windows;

namespace Ntree.ReaderTool.Light.Converter
{
    public class FalseToVisible : BoolToValueConverter<Visibility>
    {
        public override Visibility FalseValue => Visibility.Visible;
        public override Visibility TrueValue => Visibility.Collapsed;
    }
}