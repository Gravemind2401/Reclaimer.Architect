using Reclaimer.Models;
using Reclaimer.Resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Reclaimer.Utilities
{
    public class IsPaletteNodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var nodeType = value as NodeType? ?? NodeType.None;
            var action = parameter?.ToString();

            var paletteKey = PaletteType.FromNodeType(nodeType);
            if (paletteKey != null)
                return true;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
