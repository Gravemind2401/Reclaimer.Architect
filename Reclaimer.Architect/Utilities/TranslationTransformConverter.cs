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
using Adjutant.Geometry;

namespace Reclaimer.Utilities
{
    public class TranslationTransformConverter : IValueConverter
    {
        public static TranslationTransformConverter Instance { get; } = new TranslationTransformConverter();

        private TranslationTransformConverter() { }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var position = (IXMVector)value;
            var matrix = SharpDX.Matrix.Translation(position.ToVector3());
            return new Media3D.MatrixTransform3D(matrix.ToMatrix3D());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var matrix = ((Media3D.MatrixTransform3D)value).Value.ToMatrix();
            return matrix.TranslationVector.ToRealVector3D();
        }
    }
}
