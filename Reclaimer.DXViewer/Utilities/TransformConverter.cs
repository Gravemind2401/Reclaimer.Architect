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
    public class TransformConverter : IMultiValueConverter
    {
        public static TransformConverter Instance { get; } = new TransformConverter();

        private TransformConverter() { }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var position = (IRealVector3D)values[0];
            var rotation = (IRealVector3D)values[1];
            var scale = (float)values[2];

            var matrix = SharpDX.Matrix.Scaling(scale == 0 ? 1 : scale)
                * SharpDX.Matrix.RotationYawPitchRoll(rotation.Z, rotation.Y, rotation.X)
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
            result[1] = new RealVector3D(euler.X, euler.Z, euler.Y);
            result[2] = scale;
            return result;
        }
    }
}
