using HelixToolkit.Wpf.SharpDX;
using Reclaimer.Utilities;
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
    public abstract class MarkerGeometry3D : GroupElement3D, IMeshNode, IManipulatable
    {
        public MarkerGeometry3D()
        {
            Children.Add(GetMeshGeometry());
        }

        public virtual ManipulationFlags ManipulationFlags => ManipulationFlags.Default;

        public virtual float ScaleMultiplier => 1f;

        public virtual bool UseLocalOrigin => true;

        public virtual bool UniformScaling => true;

        protected abstract MeshGeometryModel3D GetMeshGeometry();

        #region IMeshNode

        string IMeshNode.Name => GetType().Name;

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
