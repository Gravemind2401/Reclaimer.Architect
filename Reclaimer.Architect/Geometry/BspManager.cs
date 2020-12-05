using Adjutant.Blam.Common;
using Adjutant.Geometry;
using Adjutant.Spatial;
using Adjutant.Utilities;
using Reclaimer.Models;
using Reclaimer.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Dds;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static HelixToolkit.Wpf.SharpDX.Media3DExtension;

using Media = System.Windows.Media;
using Media3D = System.Windows.Media.Media3D;
using Helix = HelixToolkit.Wpf.SharpDX;
using System.Windows.Data;
using Reclaimer.Controls;
using System.Collections.Concurrent;
using Reclaimer.Resources;

namespace Reclaimer.Geometry
{
    public class BspManager : IDisposable
    {
        private readonly ModelFactory factory = new ModelFactory();

        private StructureBspModel bspModel;
        private ModelProperties modelProps;

        private int tagId => bspModel.StructureBspTag.Id;

        public ObjectHolder ClusterHolder { get; private set; }
        public Dictionary<int, InstanceHolder> InstanceHolders { get; private set; }

        public IEnumerable<Task> ReadStructureBsp(StructureBspModel bspModel)
        {
            this.bspModel = bspModel;
            ClusterHolder = new ObjectHolder("Clusters");
            InstanceHolders = new Dictionary<int, InstanceHolder>();

            yield return Task.Run(() =>
            {
                factory.LoadTag(bspModel.StructureBspTag, false);
                modelProps = factory.GetProperties(tagId);

                foreach (var i in modelProps.InstanceMeshes)
                    InstanceHolders.Add(i, new InstanceHolder(i, modelProps));
            });
        }

        public void RenderStructureBsp()
        {
            if (bspModel == null)
                throw new InvalidOperationException();

            foreach(var index in modelProps.StandardMeshes)
                ClusterHolder.Elements.Add(factory.CreateModelSection(tagId, 0, index, 1));

            foreach (var holder in InstanceHolders.Values)
            {
                holder.GroupElement = new Helix.GroupModel3D();
                holder.SetCapacity(holder.Placements.Count);

                for (int i = 0; i < holder.Placements.Count; i++)
                    ConfigurePlacement(holder, i);
            }
        }

        private void RemovePlacement(InstanceHolder holder, int index)
        {
            var element = holder.Elements[index];
            if (element == null)
                return;

            holder.GroupElement.Children.Remove(element);
            element.Dispose();
            holder.Elements[index] = null;
        }

        private void ConfigurePlacement(InstanceHolder holder, int index)
        {
            RemovePlacement(holder, index);

            var placement = holder.Placements[index];

            var inst = factory.CreateModelSection(tagId, 0, placement.SectionIndex, 1);
            if (inst == null)
            {
                holder.Elements[index] = null;
                return;
            }

            BindPlacement(placement, inst);

            holder.Elements[index] = inst;
            holder.GroupElement.Children.Add(inst);
        }

        //public void RefreshObject(string paletteKey, ObjectPlacement placement, string fieldId)
        //{
        //    var holder = InstanceHolders[paletteKey];
        //    var index = holder.Definition.Placements.IndexOf(placement);

        //    if (fieldId == FieldId.Variant)
        //        (holder.Elements[index] as ObjectModel3D)?.SetVariant(placement.Variant);
        //    else if (fieldId == FieldId.PaletteIndex)
        //    {
        //        ConfigurePlacement(holder, index);

        //        var info = holder.GetInfoForIndex(index);
        //        info.TreeItem.Header = info.Placement.GetDisplayName();
        //        info.TreeItem.Tag = info.Element;

        //        var listItem = bspModel.Items.FirstOrDefault(i => i.Tag == info.Placement);
        //        if (listItem != null)
        //            listItem.Content = info.TreeItem.Header;
        //    }
        //}

        private void BindPlacement(InstancePlacement placement, Helix.Element3D model)
        {
            var binding = new MultiBinding { Converter = MatrixTransformConverter.Instance, Mode = BindingMode.TwoWay };
            binding.Bindings.Add(new Binding(nameof(InstancePlacement.TransformScale)) { Mode = BindingMode.TwoWay });
            binding.Bindings.Add(new Binding(nameof(InstancePlacement.Transform)) { Mode = BindingMode.TwoWay });

            model.DataContext = placement;
            BindingOperations.SetBinding(model, Helix.Element3D.TransformProperty, binding);
        }

        public void Dispose()
        {
            ClusterHolder?.Dispose();

            if (InstanceHolders != null)
            {
                foreach (var holder in InstanceHolders.Values)
                    holder.Dispose();
            }

            factory.Dispose();
        }
    }
}
