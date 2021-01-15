using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;

using System.Windows;
using Media3D = System.Windows.Media.Media3D;
using Media = System.Windows.Media;

namespace Reclaimer.Controls.Markers
{
    public sealed class DecalMarker3D : MarkerGeometry3D
    {
        private static readonly Material MarkerMaterial = DiffuseMaterials.LightBlue;
        private static readonly Geometry3D MarkerGeometry;

        static DecalMarker3D()
        {
            var builder = new MeshBuilder();

            var baseHeight = 0.05f;
            var halfHeight = baseHeight / 2f;
            var sideLength = 0.4f;

            builder.AddBox(Vector3.UnitZ * halfHeight, Vector3.UnitX, Vector3.UnitY, sideLength, sideLength, baseHeight);
            builder.AddCylinder(Vector3.Zero, new Vector3(0, 0, sideLength * 0.6f), halfHeight / 2f, 18, true, true);
            builder.AddArrow(new Vector3(0, 0, halfHeight), new Vector3(sideLength * 0.9f, 0, halfHeight), halfHeight, 3, 18);

            MarkerGeometry = builder.ToMesh();
        }

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
