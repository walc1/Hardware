using System.Windows;

namespace TestServer.Converter
{
    public class FalseToVisible : BoolToValueConverter<Visibility>
    {
        public override Visibility FalseValue => Visibility.Visible;
        public override Visibility TrueValue => Visibility.Collapsed;
    }
}