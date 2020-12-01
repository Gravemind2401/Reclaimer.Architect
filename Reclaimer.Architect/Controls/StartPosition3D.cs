using SharpDX;
using SharpDX.Direct3D11;
using System.Collections.Generic;
using System.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;

using System.Windows;
using Media3D = System.Windows.Media.Media3D;
using Media = System.Windows.Media;

using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Utilities;
using Reclaimer.Utilities;

namespace Reclaimer.Controls
{
    public sealed class StartPosition3D : GroupElement3D, IMeshNode
    {
        private static readonly Material PedestalMaterial = DiffuseMaterials.LightBlue;
        private static readonly Geometry3D PedestalGeometry;

        static StartPosition3D()
        {
            var builder = new MeshBuilder();

            var baseHeight = 0.075f;
            var halfHeight = baseHeight / 2f;
            var baseRadius = 0.2f;

            builder.AddCylinder(Vector3.Zero, new Vector3(0, 0, baseHeight), baseRadius, 18, true, true);
            builder.AddArrow(new Vector3(0, 0, halfHeight), new Vector3(baseRadius * 1.6f, 0, halfHeight), halfHeight, 3, 18);

            PedestalGeometry = builder.ToMesh();
        }

        public StartPosition3D()
        {
            var geom = new MeshGeometryModel3D
            {
                Geometry = PedestalGeometry,
                Material = PedestalMaterial
            };

            Children.Add(geom);
        }

        #region IMeshNode

        string IMeshNode.Name => nameof(StartPosition3D);

        bool IMeshNode.IsVisible
        {
            get { return IsRendering; }
            set { IsRendering = value; }
        }

        BoundingBox IMeshNode.GetNodeBounds()
        {
            return this.GetTotalBounds();
        }

        #endregion
    }
}
