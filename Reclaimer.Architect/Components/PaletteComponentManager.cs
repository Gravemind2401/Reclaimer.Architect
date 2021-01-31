using Reclaimer.Controls.Markers;
using Reclaimer.Geometry;
using Reclaimer.Models;
using Reclaimer.Controls;
using Reclaimer.Resources;
using Reclaimer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Xml;

using Media = System.Windows.Media;
using Media3D = System.Windows.Media.Media3D;
using Helix = HelixToolkit.Wpf.SharpDX;

namespace Reclaimer.Components
{
    public class PaletteComponentManager : ComponentManager
    {
        private readonly Dictionary<string, PaletteHolder> PaletteHolders;

        public PaletteComponentManager(ScenarioModel scenario)
            : base(scenario)
        {
            PaletteHolders = new Dictionary<string, PaletteHolder>();
        }

        public override bool HandlesNodeType(NodeType nodeType) => PaletteType.FromNodeType(nodeType) != null;

        public override Task InitializeResourcesAsync(ModelFactory factory)
        {
            return Task.WhenAll(GetResourceInitializers(factory));
        }

        private IEnumerable<Task> GetResourceInitializers(ModelFactory factory)
        {
            foreach (var definition in scenario.Palettes.Values)
            {
                //not fully implemented yet
                if (definition.Name == PaletteType.LightFixture)
                    continue;

                yield return Task.Run(() =>
                {
                    for (int i = 0; i < definition.Palette.Count; i++)
                        if (ModelFactory.IsTagSupported(definition.Palette[i].Tag))
                            factory.LoadTag(definition.Palette[i].Tag, false);
                });
            }
        }

        public override void InitializeElements(ModelFactory factory)
        {
            foreach (var definition in scenario.Palettes.Values)
            {
                var holder = new PaletteHolder(definition);
                holder.GroupElement = new Helix.GroupModel3D();
                holder.SetCapacity(holder.Definition.Placements.Count);

                for (int i = 0; i < holder.Definition.Placements.Count; i++)
                    ConfigurePlacement(factory, holder, i);

                PaletteHolders.Add(holder.Name, holder);
            }
        }

        public override IEnumerable<Helix.Element3D> GetSceneElements()
        {
            return PaletteHolders.Values.Select(h => h.GroupElement);
        }

        public override IEnumerable<TreeItemModel> GetSceneNodes()
        {
            foreach (var holder in PaletteHolders.Values)
            {
                if (holder.Name == PaletteType.Decal)
                    continue;

                var paletteNode = new TreeItemModel { Header = holder.Name, IsChecked = true };

                for (int i = 0; i < holder.Elements.Count; i++)
                {
                    var info = holder.GetInfoForIndex(i);

                    var permNode = info.TreeItem = new TreeItemModel { Header = info.Placement.GetDisplayName(), IsChecked = true, Tag = info.Element };
                    paletteNode.Items.Add(permNode);
                }

                if (paletteNode.HasItems)
                    yield return paletteNode;
            }
        }

        public override void OnSelectedTreeNodeChanged(SceneNodeModel newNode)
        {
            var nodeType = newNode?.NodeType ?? NodeType.None;

            //if the node is not a palette this will disable hit testing on all palettes
            var paletteKey = PaletteType.FromNodeType(nodeType);
            foreach (var palette in PaletteHolders.Values)
                palette.GroupElement.IsHitTestVisible = palette.Name == paletteKey;

            //only render decals when the decal node is selected
            PaletteHolders[PaletteType.Decal].GroupElement.IsRendering = nodeType == NodeType.Decals;
        }

        public override Helix.Element3D GetElement(SceneNodeModel treeNode, int itemIndex)
        {
            var nodeType = treeNode?.NodeType ?? NodeType.None;
            var paletteKey = PaletteType.FromNodeType(nodeType);
            return PaletteHolders[paletteKey].Elements[itemIndex];
        }

        public override SharpDX.BoundingBox GetObjectBounds(SceneNodeModel treeNode, int itemIndex)
        {
            var element = GetElement(treeNode, itemIndex);
            return (element as IMeshNode)?.GetNodeBounds() ?? element.GetTotalBounds();
        }

        public override int GetElementIndex(SceneNodeModel treeNode, Helix.Element3D element)
        {
            var paletteKey = PaletteType.FromNodeType(treeNode.NodeType);
            return scenario.Palettes[paletteKey].Placements.IndexOf(element.DataContext as ObjectPlacement);
        }

        public override IEnumerable<ScenarioListItem> GetListItems(SceneNodeModel treeNode)
        {
            var paletteKey = PaletteType.FromNodeType(treeNode.NodeType);
            foreach (var placement in scenario.Palettes[paletteKey].Placements)
                yield return new ScenarioListItem(placement.GetDisplayName(), placement);
        }

        internal override BlockPropertiesLocator GetPropertiesLocator(SceneNodeModel treeNode, int itemIndex)
        {
            var paletteKey = PaletteType.FromNodeType(treeNode.NodeType);
            if (itemIndex >= 0)
            {
                var palette = scenario.Palettes[paletteKey];
                var altNodes = new List<Tuple<XmlNode, long>>();
                altNodes.Add(Tuple.Create(palette.PaletteNode, scenario.RootAddress));

                return new BlockPropertiesLocator
                {
                    RootNode = palette.PlacementsNode,
                    BaseAddress = palette.PlacementBlockRef.TagBlock.Pointer.Address
                        + itemIndex * palette.PlacementBlockRef.BlockSize,
                    AdditionalNodes = altNodes,
                    TargetObject = palette.Placements[itemIndex]
                };
            }

            return null;
        }

        public override void DisposeSceneElements()
        {
            foreach (var holder in PaletteHolders.Values)
                holder.Dispose();

            PaletteHolders.Clear();
        }

        #region Binding Setup
        private void RemovePlacement(PaletteHolder holder, int index)
        {
            var element = holder.Elements[index];
            if (element == null)
                return;

            holder.GroupElement.Children.Remove(element);
            element.Dispose();
            holder.Elements[index] = null;
        }

        private void ConfigurePlacement(ModelFactory factory, PaletteHolder holder, int index)
        {
            RemovePlacement(holder, index);

            var placement = holder.Definition.Placements[index];
            var tag = placement.PaletteIndex >= 0 ? holder.Definition.Palette[placement.PaletteIndex].Tag : null;
            if (tag == null)
            {
                holder.Elements[index] = null;
                return;
            }

            Helix.Element3D inst;
            if (holder.Definition.Name == PaletteType.Decal)
                inst = new DecalMarker3D();
            else if (holder.Definition.Name == PaletteType.LightFixture)
                inst = new LightMarker3D();
            else
            {
                inst = factory.CreateObjectModel(tag.Id);
                if (inst == null)
                {
                    holder.Elements[index] = null;
                    return;
                }
            }

            BindPlacement(placement, inst);

            holder.Elements[index] = inst;
            holder.GroupElement.Children.Add(inst);
        }

        public void RefreshPalette(ModelFactory factory, string paletteKey, int index)
        {
            var holder = PaletteHolders[paletteKey];
            factory.LoadTag(holder.Definition.Palette[index].Tag, false); // in case it is new to the palette
            foreach (var placement in holder.Definition.Placements.Where(p => p.PaletteIndex == index))
                RefreshObject(factory, paletteKey, placement, FieldId.PaletteIndex);
        }

        public void RefreshObject(ModelFactory factory, string paletteKey, ObjectPlacement placement, string fieldId)
        {
            var holder = PaletteHolders[paletteKey];
            var index = holder.Definition.Placements.IndexOf(placement);

            if (fieldId == FieldId.Variant)
                (holder.Elements[index] as ObjectModel3D)?.SetVariant(placement.Variant);
            else if (fieldId == FieldId.PaletteIndex)
            {
                ConfigurePlacement(factory, holder, index);

                var info = holder.GetInfoForIndex(index);
                info.TreeItem.Header = info.Placement.GetDisplayName();
                info.TreeItem.Tag = info.Element;

                var listItem = scenario.Items.FirstOrDefault(i => i.Tag == info.Placement);
                if (listItem != null)
                    listItem.Content = info.TreeItem.Header;
            }
        }

        private void BindPlacement(ObjectPlacement placement, Helix.Element3D model)
        {
            IMultiValueConverter converter = EulerTransformConverter.Instance;
            var rotationPath = nameof(ObjectPlacement.Rotation);

            if (placement.PaletteKey == PaletteType.Decal)
            {
                converter = QuaternionTransformConverter.Instance;
                rotationPath = nameof(ObjectPlacement.QRotation);
            }

            var binding = new MultiBinding { Converter = converter, Mode = BindingMode.TwoWay };
            binding.Bindings.Add(new Binding(nameof(ObjectPlacement.Position)) { Mode = BindingMode.TwoWay });
            binding.Bindings.Add(new Binding(rotationPath) { Mode = BindingMode.TwoWay });
            binding.Bindings.Add(new Binding(nameof(ObjectPlacement.Scale)) { Mode = BindingMode.TwoWay });

            model.DataContext = placement;
            BindingOperations.SetBinding(model, Helix.Element3D.TransformProperty, binding);
        } 
        #endregion
    }
}
