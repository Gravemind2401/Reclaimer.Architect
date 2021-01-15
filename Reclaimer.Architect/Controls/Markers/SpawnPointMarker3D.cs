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
    public sealed class SpawnPointMarker3D : MarkerGeometry3D
    {
        private static readonly Material MarkerMaterial = DiffuseMaterials.LightBlue;
        private static readonly Geometry3D MarkerGeometry;

        static SpawnPointMarker3D()
        {
            var builder = new MeshBuilder();

            var baseHeight = 0.075f;
            var halfHeight = baseHeight / 2f;
            var baseRadius = 0.2f;

            builder.AddCylinder(Vector3.Zero, new Vector3(0, 0, baseHeight), baseRadius, 18, true, true);
            builder.AddArrow(new Vector3(0, 0, halfHeight), new Vector3(baseRadius * 1.6f, 0, halfHeight), halfHeight, 3, 18);

            var diamondSize = 0.3f;
            builder.AddOctahedron(new Vector3(0f, 0f, diamondSize * 1.5f), Vector3.UnitX, Vector3.UnitZ, diamondSize, diamondSize);
            builder.AddOctahedron(new Vector3(0f, 0f, diamondSize * 0.5f), Vector3.UnitX, -Vector3.UnitZ, diamondSize, diamondSize);

            MarkerGeometry = builder.ToMesh();
        }

        public override ManipulationFlags ManipulationFlags => ManipulationFlags.Translate | ManipulationFlags.Rotate;

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
