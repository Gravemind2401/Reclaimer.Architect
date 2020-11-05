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
using Reclaimer.Geometry;
using Reclaimer.Controls;

namespace Reclaimer.Utilities
{
    public static class Extensions
    {
        public static SharpDX.Vector2 ToVector2(this IXMVector v) => new SharpDX.Vector2(v.X, v.Y);

        public static SharpDX.Vector3 ToVector3(this IXMVector v) => new SharpDX.Vector3(v.X, v.Y, v.Z);

        public static SharpDX.Vector3 ToVector3(this IRealVector3D v) => new SharpDX.Vector3(v.X, v.Y, v.Z);

        public static RealVector3D ToRealVector3D(this SharpDX.Vector3 v) => new RealVector3D(v.X, v.Y, v.Z);

        public static SharpDX.Quaternion ToQuaternion(this IRealVector4D v) => new SharpDX.Quaternion(v.X, v.Y, v.Z, v.W);

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

        public static Numerics.Vector3 ToNumericsVector3(this Media3D.Point3D point)
        {
            return new Numerics.Vector3((float)point.X, (float)point.Y, (float)point.Z);
        }

        public static Numerics.Vector3 ToNumericsVector3(this Media3D.Vector3D vector)
        {
            return new Numerics.Vector3((float)vector.X, (float)vector.Y, (float)vector.Z);
        }

        public static SharpDX.Vector3 ToEulerAngles(this SharpDX.Quaternion q)
        {
            var matrix = SharpDX.Matrix.RotationQuaternion(q);
            var result = new SharpDX.Vector3();

            result.Y = (float)Math.Asin(Clamp(-matrix.M32, -1, 1));
            if (Math.Abs(matrix.M32) < 0.9999999)
            {
                result.X = (float)Math.Atan2(matrix.M12, matrix.M22);
                result.Z = (float)Math.Atan2(matrix.M31, matrix.M33);
            }
            else
            {
                result.X = (float)Math.Atan2(matrix.M21, matrix.M11);
                result.Z = 0f;
            }

            return result;
        }

        private static float Clamp(float value, float min, float max) => Math.Min(Math.Max(min, value), max);

        public static bool IsDescendentOf(this Helix.Element3D element, Helix.Element3D target)
        {
            var parent = element.Parent as Helix.Element3D;
            if (parent == null)
                return false;
            else if (parent == target)
                return true;
            else return IsDescendentOf(parent, target);
        }

        public static Helix.Element3D FindInstanceParent(this Helix.Element3D element)
        {
            if (element.Tag is IModelInstance)
                return element;

            return element.EnumerateAncestors().Reverse().FirstOrDefault(e => e.Tag is IModelInstance);
        }

        public static IEnumerable<Helix.Element3D> EnumerateAncestors(this Helix.Element3D element)
        {
            while (element.Parent != null)
            {
                var parent = element.Parent as Helix.Element3D;
                if (parent == null)
                    break;

                yield return parent;
                element = parent;
            }
        }

        public static IEnumerable<Helix.Element3D> EnumerateDescendents(this Helix.GroupElement3D group, bool invisible = false)
        {
            if (group.Children.Count == 0)
                return Enumerable.Empty<Helix.Element3D>();

            var validChildren = group.Children.Where(e => invisible || e.Visible);

            var descendents = validChildren;
            foreach (var branch in validChildren.OfType<Helix.GroupElement3D>())
                descendents = descendents.Concat(branch.EnumerateDescendents());

            return descendents;
        }

        public static SharpDX.BoundingBox GetTotalBounds(this Helix.Element3D element, bool original = false)
        {
            return GetTotalBounds(Enumerable.Repeat(element, 1), original);
        }

        public static SharpDX.BoundingBox GetTotalBounds(this IEnumerable<Helix.Element3D> elements, bool original = false)
        {
            var boundsList = new List<SharpDX.BoundingBox>();
            foreach (var element in elements)
                CollectChildBounds(element.SceneNode, boundsList, original);

            return boundsList.GetTotalBounds();
        }

        public static SharpDX.BoundingBox GetTotalBounds(this IEnumerable<SharpDX.BoundingBox> bounds)
        {
            var boundsList = bounds as IList<SharpDX.BoundingBox> ?? bounds.ToList();

            if (!boundsList.Any())
                return default(SharpDX.BoundingBox);

            var min = new SharpDX.Vector3(
                boundsList.Min(b => b.Minimum.X),
                boundsList.Min(b => b.Minimum.Y),
                boundsList.Min(b => b.Minimum.Z)
            );

            var max = new SharpDX.Vector3(
                boundsList.Max(b => b.Maximum.X),
                boundsList.Max(b => b.Maximum.Y),
                boundsList.Max(b => b.Maximum.Z)
            );

            return new SharpDX.BoundingBox(min, max);
        }

        private static void CollectChildBounds(Helix.Model.Scene.SceneNode node, List<SharpDX.BoundingBox> boundsList, bool original)
        {
            if (node.HasBound)
                boundsList.Add(original ? node.OriginalBounds : node.BoundsWithTransform);
            else if (node.ItemsCount > 0)
            {
                foreach (var child in node.Items.Where(i => i.Visible))
                    CollectChildBounds(child, boundsList, original);
            }
        }

        public static T[] ToArray<T>(this IEnumerable<T> source, int size)
        {
            var result = new T[size];

            int i = 0;
            foreach (var item in source)
                result[i++] = item;

            return result;
        }

        public static void SetVisibility(this IMeshNode node, bool isVisible)
        {
            node.IsVisible = isVisible;
        }
    }
}
