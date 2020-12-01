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
    public class TransformConverter : IMultiValueConverter
    {
        public static TransformConverter Instance { get; } = new TransformConverter();

        private TransformConverter() { }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var position = (IXMVector)values[0];
            var rotation = (IXMVector)values[1];
            var scale = values.Length > 2 ? (float)values[2] : 1f;

            var euler = new SharpDX.Vector3(rotation.X, rotation.Y, float.IsNaN(rotation.Z) ? 0f : rotation.Z);

            var matrix = SharpDX.Matrix.Scaling(scale == 0 ? 1 : scale)
                * SharpDX.Matrix.RotationQuaternion(euler.EulerToQuaternion())
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
            var euler = rotation.ToEulerAngles();

            var result = new object[3];
            result[0] = position.ToRealVector3D();
            result[1] = targetTypes[1] == typeof(RealVector2D) 
                ? (object)new RealVector2D(euler.X, euler.Y)
                : (object)new RealVector3D(euler.X, euler.Y, euler.Z);
            result[2] = scale;
            return result;
        }
    }
}
