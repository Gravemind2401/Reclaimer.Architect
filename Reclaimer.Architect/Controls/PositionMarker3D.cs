using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;

using System.Windows;
using Media3D = System.Windows.Media.Media3D;
using Media = System.Windows.Media;

namespace Reclaimer.Controls
{
    public sealed class PositionMarker3D : MarkerGeometry3D
    {
        private static readonly Material MarkerMaterial = DiffuseMaterials.Red;
        private static readonly Geometry3D MarkerGeometry;

        static PositionMarker3D()
        {
            var builder = new MeshBuilder();

            var baseHeight = 0.075f;
            var halfHeight = baseHeight / 2;
            var sideLength = 0.75f;

            builder.AddBox(Vector3.UnitZ * halfHeight, Vector3.UnitX, Vector3.UnitY, sideLength, sideLength, baseHeight);

            MarkerGeometry = builder.ToMesh();
        }

        public override ManipulationFlags ManipulationFlags => ManipulationFlags.Translate;

        protected override MeshGeometryModel3D GetMeshGeometry()
        {
            return new MeshGeometryModel3D
            {
                Geometry = MarkerGeometry,
                Material = MarkerMaterial
            };
        }
    }
}
