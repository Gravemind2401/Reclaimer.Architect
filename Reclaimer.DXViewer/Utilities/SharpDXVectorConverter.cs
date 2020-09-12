using Adjutant.Spatial;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using static HelixToolkit.Wpf.SharpDX.Media3DExtension;

using Media3D = System.Windows.Media.Media3D;
using Helix = HelixToolkit.Wpf.SharpDX;

namespace Reclaimer.Utilities
{
    public class SharpDXVectorConverter : IValueConverter
    {
        public static SharpDXVectorConverter Instance { get; } = new SharpDXVectorConverter();

        private SharpDXVectorConverter() { }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IRealVector3D)
                return ((IRealVector3D)value).ToVector3();

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SharpDX.Vector3)
                return ((SharpDX.Vector3)value).ToRealVector3D();

            return null;
        }
    }
}
