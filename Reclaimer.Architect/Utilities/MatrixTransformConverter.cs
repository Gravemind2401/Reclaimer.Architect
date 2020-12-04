using Adjutant.Spatial;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using static HelixToolkit.Wpf.SharpDX.Media3DExtension;

using Numerics = System.Numerics;
using Media3D = System.Windows.Media.Media3D;
using Helix = HelixToolkit.Wpf.SharpDX;

namespace Reclaimer.Utilities
{
    public class MatrixTransformConverter : IMultiValueConverter
    {
        public static MatrixTransformConverter Instance { get; } = new MatrixTransformConverter();

        private MatrixTransformConverter() { }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var scale = (float)values[0];
            var transform = (Numerics.Matrix4x4)values[1];

            var tGroup = new Media3D.Transform3DGroup();

            if (scale != 1)
            {
                var tform = new Media3D.ScaleTransform3D(scale, scale, scale);

                tform.Freeze();
                tGroup.Children.Add(tform);
            }

            if (!transform.IsIdentity)
            {
                var tform = new Media3D.MatrixTransform3D(transform.ToMatrix3D());

                tform.Freeze();
                tGroup.Children.Add(tform);
            }

            tGroup.Freeze();
            return tGroup;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            var matrix = ((Media3D.MatrixTransform3D)value).Value.ToMatrix();

            float scale;
            SharpDX.Quaternion rotation;
            SharpDX.Vector3 position;

            matrix.DecomposeUniformScale(out scale, out rotation, out position);

            var result = new object[2];
            result[0] = scale;
            result[1] = (SharpDX.Matrix.RotationQuaternion(rotation) * SharpDX.Matrix.Translation(position)).ToNumericsMatrix4x4();

            return result;
        }
    }
}
