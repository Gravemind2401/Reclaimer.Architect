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
using Prism.Mvvm;
using Reclaimer.Geometry;
using Reclaimer.Models;

namespace Reclaimer.Controls
{
    public sealed class ObjectModel3D : GroupElement3D, IMeshNode
    {
        private readonly ModelConfig config;
        private readonly RenderModel3D baseModel;
        private readonly VariantConfig defaultVariant;
        private readonly Dictionary<string, List<ObjectModel3D>> attachments;

        public string ModelName { get; }
        public string DefaultVariant => defaultVariant?.Name;
        public IEnumerable<string> Variants => config.Variants.Select(v => v.Name);

        public ObjectModel3D(ModelFactory factory, ModelConfig config, string name, string defaultVariant)
        {
            this.config = config;
            ModelName = name;
            this.defaultVariant = config.Variants.FirstOrDefault(v => v.Name == defaultVariant) ?? config.Variants.FirstOrDefault();
            attachments = new Dictionary<string, List<ObjectModel3D>>();

            if (config.RenderModelTag == null)
                baseModel = RenderModel3D.Error("null");
            else
                baseModel = factory.CreateRenderModel(config.RenderModelTag.Id);

            foreach (var variant in config.Variants)
            {
                var children = new List<ObjectModel3D>();
                foreach (var attachment in variant.Attachments.Where(att => att.ChildTag != null))
                {
                    var child = factory.CreateObjectModel(attachment.ChildTag.Id);
                    if (child.config.RenderModelTag == null)
                        continue;

                    var parentProps = factory.GetProperties(config.RenderModelTag.Id);
                    var childProps = factory.GetProperties(child.config.RenderModelTag.Id);
                    child.Transform = GetAttachmentTransform(parentProps, attachment.ParentMarker, childProps, attachment.ChildMarker);

                    child.SetVariant(attachment.ChildVariant);
                    children.Add(child);
                }

                attachments.Add(variant.Name, children);
            }

            SetVariant(DefaultVariant);
        }

        private Media3D.MatrixTransform3D GetAttachmentTransform(ModelProperties p1, string m1, ModelProperties p2, string m2)
        {
            var t1 = GetMarkerTransform(p1, m1);
            var t2 = GetMarkerTransform(p2, m2);

            if (t2.IsIdentity)
                return new Media3D.MatrixTransform3D(t1.ToMatrix3D());

            t2.Invert();
            return new Media3D.MatrixTransform3D((t1 * t2).ToMatrix3D());
        }

        private Matrix GetMarkerTransform(ModelProperties model, string markerName)
        {
            var marker = model.MarkerGroups.FirstOrDefault(g => g.Name == markerName)?.Markers.FirstOrDefault();
            if (marker == null)
                return Matrix.Identity;

            var transfoms = new List<Matrix>();

            if (marker.NodeIndex != byte.MaxValue)
            {
                int parentIndex = marker.NodeIndex;
                while (parentIndex >= 0)
                {
                    var node = model.Nodes[parentIndex];
                    var t = Matrix.RotationQuaternion(node.Rotation.ToQuaternion());
                    t.TranslationVector = node.Position.ToVector3();
                    transfoms.Insert(0, t);
                    parentIndex = node.ParentIndex;
                }
            }

            var mat = Matrix.RotationQuaternion(marker.Rotation.ToQuaternion());
            mat.TranslationVector = marker.Position.ToVector3();
            transfoms.Add(mat);

            return transfoms.Aggregate((m1, m2) => m1 * m2);
        }

        public void SetVariant(string variantName)
        {
            variantName = variantName ?? DefaultVariant;

            var variant = config.Variants.FirstOrDefault(v => v.Name.Equals(variantName, StringComparison.OrdinalIgnoreCase)) ?? defaultVariant;
            if (variant == null)
            {
                Children.Clear();
                baseModel.ShowAll();
                Children.Add(baseModel);
                return;
            }

            Children.Clear();
            baseModel.ApplyVariant(variant);
            Children.Add(baseModel);

            foreach (var att in attachments[variant.Name])
                Children.Add(att);
        }

        protected override SceneNode OnCreateSceneNode()
        {
            return new ViewDistanceGroupNode(this);
        }

        #region IMeshNode

        string IMeshNode.Name => ModelName;

        bool IMeshNode.IsVisible
        {
            get { return IsRendering; }
            set { IsRendering = value; }
        }

        BoundingBox IMeshNode.GetNodeBounds()
        {
            return ((IMeshNode)baseModel).GetNodeBounds();
        }

        #endregion
    }
}
