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

        public override void InitializeElements(ModelFactory factory)
        {
            StartPositionGroup = new Helix.GroupModel3D();
            foreach (var pos in scenario.StartingPositions)
            {
                var element = new StartPositionMarker3D();
                BindStartPosition(pos, element);
                StartPositions.Add(element);
                StartPositionGroup.Children.Add(element);
            }
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
            return scenario.StartingPositions.Select(pos => new ScenarioListItem(pos.GetDisplayName(), pos));
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
