using Adjutant.Geometry;
using Adjutant.Spatial;
using Reclaimer.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Numerics = System.Numerics;
using Media3D = System.Windows.Media.Media3D;
using Helix = HelixToolkit.Wpf.SharpDX;
using Reclaimer.Utilities.IO;
using Reclaimer.Models;
using Adjutant.Blam.Common;

namespace Reclaimer.Utilities
{
    public static class Extensions
    {
        public static SharpDX.Vector2 ToVector2(this IXMVector v) => new SharpDX.Vector2(v.X, v.Y);

        public static SharpDX.Vector3 ToVector3(this IXMVector v) => new SharpDX.Vector3(v.X, v.Y, v.Z);

        public static SharpDX.Vector3 ToVector3(this IRealVector3D v) => new SharpDX.Vector3(v.X, v.Y, v.Z);

        public static RealVector3D ToRealVector3D(this Media3D.Point3D p) => new RealVector3D((float)p.X, (float)p.Y, (float)p.Z);

        public static RealVector3D ToRealVector3D(this SharpDX.Vector3 v) => new RealVector3D(v.X, v.Y, v.Z);

        public static RealVector4D ToRealVector4D(this SharpDX.Quaternion q) => new RealVector4D(q.X, q.Y, q.Z, q.W);

        public static SharpDX.Quaternion ToQuaternion(this IRealVector4D v) => new SharpDX.Quaternion(v.X, v.Y, v.Z, v.W);

        public static SharpDX.Quaternion ToQuaternion(this IXMVector q) => new SharpDX.Quaternion(q.X, q.Y, q.Z, q.W);

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

        public static Numerics.Matrix4x4 ToNumericsMatrix4x4(this SharpDX.Matrix m)
        {
            return new Numerics.Matrix4x4
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

                M41 = m.M41,
                M42 = m.M42,
                M43 = m.M43
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

        public static SharpDX.Quaternion EulerToQuaternion(this SharpDX.Vector3 v)
        {
            //Halo seems to apply in order of roll, pitch, yaw.
            //Not sure why Y is negated; there may be difference
            //in coordinate systems between Halo and SharpDX?

            return SharpDX.Quaternion.RotationAxis(SharpDX.Vector3.UnitX, v.Z)
                * SharpDX.Quaternion.RotationAxis(SharpDX.Vector3.UnitY, -v.Y)
                * SharpDX.Quaternion.RotationAxis(SharpDX.Vector3.UnitZ, v.X);
        }

        public static SharpDX.Vector3 ToEulerAngles(this SharpDX.Quaternion q)
        {
            //https://github.com/mrdoob/three.js/blob/dev/src/math/Euler.js
            //Due to the issue mentioned in EulerToQuaternion, X and Z are negated to compensate.

            var matrix = SharpDX.Matrix.RotationQuaternion(q);
            var result = new SharpDX.Vector3();

            result.Y = (float)Math.Asin(-Clamp(matrix.M31, -1, 1));
            if (Math.Abs(matrix.M31) < 0.9999999)
            {
                result.X = -(float)Math.Atan2(matrix.M21, matrix.M11);
                result.Z = -(float)Math.Atan2(matrix.M32, matrix.M33);
            }
            else
            {
                result.X = -(float)Math.Atan2(-matrix.M12, matrix.M22);
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
            if (element is IMeshNode)
                return element;

            return element.EnumerateAncestors().Reverse().FirstOrDefault(e => e is IMeshNode);
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
            return GetTotalBounds(Enumerable.Repeat(element.SceneNode, 1), original);
        }

        public static SharpDX.BoundingBox GetTotalBounds(this IEnumerable<Helix.Element3D> elements, bool original = false)
        {
            return GetTotalBounds(elements.Select(e => e.SceneNode), original);
        }

        public static SharpDX.BoundingBox GetTotalBounds(this Helix.Model.Scene.SceneNode node, bool original = false)
        {
            return GetTotalBounds(Enumerable.Repeat(node, 1), original);
        }

        public static SharpDX.BoundingBox GetTotalBounds(this IEnumerable<Helix.Model.Scene.SceneNode> nodes, bool original = false)
        {
            var boundsList = new List<SharpDX.BoundingBox>();
            foreach (var node in nodes)
                CollectChildBounds(node, boundsList, original);

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

        public static SharpDX.RectangleF Project(this SharpDX.BoundingBox bounds, Helix.RenderContext context)
        {
            var corners = bounds.GetCorners();
            for (int i = 0; i < corners.Length; i++)
                corners[i] = SharpDX.Vector3.Project(corners[i], 0, 0, context.ActualWidth, context.ActualHeight, 0, 1, context.GlobalTransform.ViewProjection);

            return new SharpDX.RectangleF
            {
                Left = corners.Min(v => v.X),
                Top = corners.Min(v => v.Y),
                Right = corners.Max(v => v.X),
                Bottom = corners.Max(v => v.Y),
            };
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

        public static void Resize<T>(this List<T> list, int newSize)
        {
            var prevSize = list.Count;
            if (newSize < prevSize)
                list.RemoveRange(newSize, prevSize - newSize);
            else if (newSize > prevSize)
            {
                if (newSize > list.Capacity)
                    list.Capacity = newSize;
                list.AddRange(Enumerable.Repeat(default(T), newSize - prevSize));
            }
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.Where(i => i != null);
        }

        public static void UpdateBlockReference(this IBlockEditor blockEditor, IBlockReference blockRef)
        {
            blockRef.TagBlock = new TagBlock(blockEditor.EntryCount, blockRef.TagBlock.Pointer);
        }
    }
}
