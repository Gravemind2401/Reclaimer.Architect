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
    public class QuaternionTransformConverter : IMultiValueConverter
    {
        public static QuaternionTransformConverter Instance { get; } = new QuaternionTransformConverter();

        private QuaternionTransformConverter() { }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var position = (IXMVector)values[0];
            var rotation = (IXMVector)values[1];
            var scale = values.Length > 2 ? (float)values[2] : 1f;

            var matrix = SharpDX.Matrix.Scaling(scale == 0 ? 1 : scale)
                * SharpDX.Matrix.RotationQuaternion(rotation.ToQuaternion())
                * SharpDX.Matrix.Translation(position.ToVector3());

            return new Media3D.MatrixTransform3D(matrix.ToMatrix3D());
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            var matrix = ((Media3D.MatrixTransform3D)value).Value.ToMatrix();

            SharpDX.Vector3 position;
            SharpDX.Quaternion rotation;
            float scale;

            matrix.DecomposeUniformScale(out scale, out rotation, out position);

            var result = new object[3];
            result[0] = position.ToRealVector3D();
            result[1] = rotation.ToRealVector4D();
            result[2] = scale;
            return result;
        }
    }
}
