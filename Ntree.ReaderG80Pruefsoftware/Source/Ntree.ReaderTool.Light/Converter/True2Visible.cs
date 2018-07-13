using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Ntree.ReaderTool.Light.Converter
{
    public class TrueToVisible : BoolToValueConverter<Visibility>
    {
        public override Visibility FalseValue => Visibility.Collapsed;
        public override Visibility TrueValue => Visibility.Visible;
    }
}
