using Adjutant.Geometry;
using Adjutant.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Numerics = System.Numerics;
using Media3D = System.Windows.Media.Media3D;
using Helix = HelixToolkit.Wpf.SharpDX;

namespace Reclaimer.Utilities
{
    public static class Extensions
    {
        public static SharpDX.Vector2 ToVector2(this IXMVector v) => new SharpDX.Vector2(v.X, v.Y);

        public static SharpDX.Vector3 ToVector3(this IXMVector v) => new SharpDX.Vector3(v.X, v.Y, v.Z);

        public static SharpDX.Matrix ToMatrix3(this IRealBounds5D bounds)
        {
            return new SharpDX.Matrix
            {
                M11 = bounds.XBounds.Length,
                M22 = bounds.YBounds.Length,
                M33 = bounds.ZBounds.Length,
                M41 = bounds.XBounds.Min,
                M42 = bounds.YBounds.Min,
                M43 = bounds.ZBounds.Min,
                M44 = 1
            };
        }

        public static SharpDX.Matrix ToMatrix2(this IRealBounds5D bounds)
        {
            return new SharpDX.Matrix
            {
                M11 = bounds.UBounds.Length,
                M22 = bounds.VBounds.Length,
                M41 = bounds.UBounds.Min,
                M42 = bounds.VBounds.Min,
                M44 = 1
            };
        }

        public static Media3D.Matrix3D ToMatrix3D(this Numerics.Matrix4x4 m)
        {
            return new Media3D.Matrix3D
            {
                M11 = m.M11,
                M12 = m.M12,
                M13 = m.M13,

                M21 = m.M21,
                M22 = m.M22,
                M23 = m.M23,

                M31 = m.M31,
                M32 = m.M32,
                M33 = m.M33,

                OffsetX = m.M41,
                OffsetY = m.M42,
                OffsetZ = m.M43
            };
        }

        public static IEnumerable<Helix.Element3D> EnumerateDescendents(this Helix.GroupElement3D group)
        {
            if (group.Children.Count == 0)
                return Enumerable.Empty<Helix.Element3D>();

            var descendents = group.Children.AsEnumerable();
            foreach (var branch in group.Children.OfType<Helix.GroupElement3D>())
                descendents = descendents.Concat(branch.EnumerateDescendents());

            return descendents;
        }
    }
}
