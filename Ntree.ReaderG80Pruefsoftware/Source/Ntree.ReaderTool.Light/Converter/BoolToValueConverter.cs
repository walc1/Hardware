using System;
using System.Windows.Data;

namespace Ntree.ReaderTool.Light.Converter
{
    public abstract class BoolToValueConverter<T> : IValueConverter
    {
        public abstract T FalseValue { get; }
        public abstract T TrueValue { get; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return FalseValue;
            return (bool)value ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value != null && value.Equals(TrueValue);
        }
    }
}