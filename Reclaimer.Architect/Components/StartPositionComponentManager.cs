using Reclaimer.Controls.Markers;
using Reclaimer.Geometry;
using Reclaimer.Models;
using Reclaimer.Resources;
using Reclaimer.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Helix = HelixToolkit.Wpf.SharpDX;

namespace Reclaimer.Components
{
    public class StartPositionComponentManager : ComponentManager
    {
        private readonly ObservableCollection<StartPositionMarker3D> StartPositions;

        private Helix.GroupElement3D StartPositionGroup;

        public StartPositionComponentManager(ScenarioModel scenario)
            : base(scenario)
        {
            StartPositions = new ObservableCollection<StartPositionMarker3D>();
        }

        public override bool HandlesNodeType(NodeType nodeType) => nodeType == NodeType.StartPositions;

        public override bool SupportsObjectOperation(ObjectOperation operation, NodeType nodeType) => nodeType == NodeType.StartPositions;

        public override void InitializeElements(ModelFactory factory)
        {
            StartPositionGroup = new Helix.GroupModel3D();
            foreach (var pos in scenario.StartingPositions)
                AddStartPositionElement(pos);
        }

        public override IEnumerable<Helix.Element3D> GetSceneElements()
        {
            if (StartPositionGroup.Children.Any())
                yield return StartPositionGroup;
        }

        public override void OnSelectedTreeNodeChanged(SceneNodeModel newNode)
        {
            StartPositionGroup.IsRendering = newNode?.NodeType == NodeType.StartPositions;
        }

        public override Helix.Element3D GetElement(SceneNodeModel treeNode, int itemIndex)
        {
            return StartPositions[itemIndex];
        }

        public override int GetElementIndex(SceneNodeModel treeNode, Helix.Element3D element)
        {
            return scenario.StartingPositions.IndexOf(element.DataContext as StartPosition);
        }

        public override SharpDX.BoundingBox GetObjectBounds(SceneNodeModel treeNode, int itemIndex)
        {
            return GetElement(treeNode, itemIndex).GetTotalBounds();
        }

        public override IEnumerable<ScenarioListItem> GetListItems(SceneNodeModel treeNode)
        {
            return scenario.StartingPositions.Select(pos => new ScenarioListItem(pos));
        }

        internal override BlockPropertiesLocator GetPropertiesLocator(SceneNodeModel treeNode, int itemIndex)
        {
            if (itemIndex < 0)
                return null;

            var section = scenario.Sections[Section.StartPositions];
            return new BlockPropertiesLocator
            {
                RootNode = section.Node,
                BaseAddress = section.TagBlock.Pointer.Address
                    + itemIndex * section.BlockSize,
                TargetObject = scenario.Items[itemIndex]
            };
        }

        public override bool ExecuteObjectOperation(SceneNodeModel treeNode, ObjectOperation operation, int itemIndex)
        {
            var blockRef = scenario.Sections[Section.StartPositions];
            var blockEditor = scenario.MetadataStream.GetBlockEditor(blockRef.TagBlock.Pointer.Address);

            switch (operation)
            {
                case ObjectOperation.Add:
                    blockEditor.Add();
                    var pos = new StartPosition(scenario);
                    scenario.StartingPositions.Add(pos);
                    AddStartPositionElement(pos);
                    break;

                case ObjectOperation.Remove:
                    if (itemIndex < 0 || itemIndex >= blockRef.TagBlock.Count)
                        return false;

                    blockEditor.Remove(itemIndex);
                    scenario.StartingPositions.RemoveAt(itemIndex);
                    RemoveStartPositionElement(itemIndex);
                    break;

                case ObjectOperation.Copy:
                    if (itemIndex < 0 || itemIndex >= blockRef.TagBlock.Count)
                        return false;

                    var destIndex = itemIndex + 1;
                    blockEditor.Copy(itemIndex, destIndex);
                    pos = new StartPosition(scenario);
                    scenario.StartingPositions.Insert(destIndex, pos);
                    InsertStartPositionElement(pos, destIndex);
                    pos.CopyFrom(scenario.StartingPositions[itemIndex]);
                    break;
            }

            blockEditor.UpdateBlockReference(blockRef);
            return true;
        }

        private void AddStartPositionElement(StartPosition pos) => InsertStartPositionElement(pos, StartPositions.Count);

        private void InsertStartPositionElement(StartPosition pos, int index)
        {
            var element = new StartPositionMarker3D();
            BindStartPosition(pos, element);
            StartPositions.Insert(index, element);
            StartPositionGroup.Children.Add(element);
        }

        private void RemoveStartPositionElement(int index)
        {
            var element = StartPositions[index];
            StartPositions.Remove(element);
            StartPositionGroup.Children.Remove(element);
            element.Dispose();
        }

        private void BindStartPosition(StartPosition pos, Helix.Element3D model)
        {
            var binding = new MultiBinding { Converter = EulerTransformConverter.Instance, Mode = BindingMode.TwoWay };
            binding.Bindings.Add(new Binding(nameof(StartPosition.Position)) { Mode = BindingMode.TwoWay });
            binding.Bindings.Add(new Binding(nameof(StartPosition.Orientation)) { Mode = BindingMode.TwoWay });

            model.DataContext = pos;
            BindingOperations.SetBinding(model, Helix.Element3D.TransformProperty, binding);
        }

        public override void DisposeSceneElements()
        {
            StartPositionGroup?.Dispose();
            StartPositions.Clear();
            StartPositionGroup = null;
        }
    }
}
