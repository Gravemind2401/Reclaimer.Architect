using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Model.Scene;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Reclaimer.Utilities;

using Media3D = System.Windows.Media.Media3D;
using System.Windows.Data;

namespace Reclaimer.Controls
{
    public class ElementHighlighter3D : GroupElement3D
    {
        #region Dependency Properties

        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register(nameof(Target), typeof(Element3D), typeof(ElementHighlighter3D), new PropertyMetadata(null, (d, e) =>
            {
                (d as ElementHighlighter3D).OnTargetChanged(e.NewValue as Element3D);
            }));

        public static readonly DependencyProperty HighlightColorProperty =
            DependencyProperty.Register(nameof(HighlightColor), typeof(Color), typeof(ElementHighlighter3D), new PropertyMetadata(Color.Yellow, (d, e) =>
            {
                (d as ElementHighlighter3D).material.DiffuseColor = (Color)e.NewValue;
            }));

        public static readonly DependencyProperty EnableXRayGridProperty =
            DependencyProperty.Register(nameof(EnableXRayGrid), typeof(bool), typeof(ElementHighlighter3D), new PropertyMetadata(true, (d, e) =>
            {
                (d as ElementHighlighter3D).xrayEffect.IsRendering = (bool)e.NewValue;
            }));

        public Element3D Target
        {
            get { return (Element3D)GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        public Color HighlightColor
        {
            get { return (Color)GetValue(HighlightColorProperty); }
            set { SetValue(HighlightColorProperty, value); }
        }

        public bool EnableXRayGrid
        {
            get { return (bool)GetValue(EnableXRayGridProperty); }
            set { SetValue(EnableXRayGridProperty, value); }
        }

        #endregion

        private readonly GroupModel3D meshGroup;
        private readonly DiffuseMaterial material;
        private readonly Element3D xrayEffect;

        public ElementHighlighter3D()
        {
            meshGroup = new GroupModel3D();
            material = new DiffuseMaterial { DiffuseColor = HighlightColor };
            xrayEffect = new PostEffectMeshXRayGrid()
            {
                EffectName = "HighlighterXRayGrid",
                DimmingFactor = 0.5,
                BlendingFactor = 0.8,
                GridDensity = 4,
                GridColor = System.Windows.Media.Colors.Gray
            };
            (xrayEffect.SceneNode as NodePostEffectXRayGrid).XRayDrawingPassName = DefaultPassNames.EffectMeshDiffuseXRayGridP3;

            Children.Add(meshGroup);
            Children.Add(xrayEffect);

            var binding = new Binding("Target.Transform") { RelativeSource = RelativeSource.Self };
            BindingOperations.SetBinding(this, TransformProperty, binding);
        }

        private void OnTargetChanged(Element3D target)
        {
            foreach (var element in meshGroup.Children)
                element.Dispose();

            meshGroup.Children.Clear();
            //(Parent as GroupElement3D)?.Children.Remove(this);

            if (target == null)
                return;

            var allMeshes = ((target as GroupElement3D)?.EnumerateDescendents() ?? Enumerable.Repeat(target, 1)).OfType<MeshGeometryModel3D>().ToList();
            if (!allMeshes.Any())
                return;

            foreach (var m in allMeshes)
            {
                var transform = m.Transform.Value;
                var ancestors = m.EnumerateAncestors()
                    .TakeWhile(e => e != target)
                    .ToList();

                if (ancestors.Count > 0)
                {
                    transform *= ancestors
                        .Select(e => e.Transform.Value)
                        .Aggregate((a, b) => a * b);
                }

                meshGroup.Children.Add(new MeshGeometryModel3D
                {
                    CullMode = SharpDX.Direct3D11.CullMode.Back,
                    DepthBias = -100,
                    Geometry = m.Geometry,
                    Material = material,
                    IsHitTestVisible = false,
                    Transform = new Media3D.MatrixTransform3D(transform),
                    PostEffects = "HighlighterXRayGrid"
                });
            }

            //(target as GroupElement3D)?.Children.Add(this);
        }
    }
}
