using Adjutant.Spatial;
using Reclaimer.Controls;
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
    public class TriggerVolumeComponentManager : ComponentManager
    {
        private readonly TreeItemModel sceneNode;
        private readonly ObservableCollection<BoxManipulator3D> TriggerVolumes;

        private Helix.GroupElement3D TriggerVolumeGroup;

        public TriggerVolumeComponentManager(ScenarioModel scenario)
            : base(scenario)
        {
            sceneNode = new TreeItemModel { Header = "trigger volumes", IsChecked = true };
            TriggerVolumes = new ObservableCollection<BoxManipulator3D>();
        }

        public override bool HandlesNodeType(NodeType nodeType) => nodeType == NodeType.TriggerVolumes;

        public override bool SupportsObjectOperation(ObjectOperation operation, NodeType nodeType) => nodeType == NodeType.TriggerVolumes;

        public override void InitializeElements(ModelFactory factory)
        {
            TriggerVolumeGroup = new Helix.GroupModel3D();
            foreach (var vol in scenario.TriggerVolumes)
                AddTriggerVolumeElement(vol);
        }

        public override IEnumerable<Helix.Element3D> GetSceneElements()
        {
            if (TriggerVolumeGroup.Children.Any())
                yield return TriggerVolumeGroup;
        }

        public override IEnumerable<TreeItemModel> GetSceneNodes()
        {
            sceneNode.Items.Clear();

            foreach (var tv in TriggerVolumes)
            {
                var permNode = new TreeItemModel { Header = (tv.DataContext as TriggerVolume).Name, IsChecked = true, Tag = tv };
                sceneNode.Items.Add(permNode);
            }

            yield return sceneNode;
        }

        public override void OnSelectedTreeNodeChanged(SceneNodeModel newNode)
        {
            TriggerVolumeGroup.IsRendering = newNode?.NodeType == NodeType.TriggerVolumes;

            var editor = scenario.RenderView as DXEditor;
            if (newNode?.NodeType == NodeType.TriggerVolumes && !editor.TreeViewItems.Contains(sceneNode))
                editor.TreeViewItems.Add(sceneNode);
            else if (newNode?.NodeType != NodeType.TriggerVolumes && editor.TreeViewItems.Contains(sceneNode))
                editor.TreeViewItems.Remove(sceneNode);
        }

        public override Helix.Element3D GetElement(SceneNodeModel treeNode, int itemIndex)
        {
            var prev = TriggerVolumes.FirstOrDefault(t => t.Tag != null);
            if (prev != null)
            {
                prev.DiffuseColor = TriggerVolume.DefaultColour;
                prev.Tag = null;
            }

            var selected = TriggerVolumes[itemIndex];
            selected.DiffuseColor = TriggerVolume.SelectedColour;
            selected.Tag = true;

            RefreshTriggerVolumes(itemIndex);

            return selected;
        }

        public override int GetElementIndex(SceneNodeModel treeNode, Helix.Element3D element)
        {
            return scenario.TriggerVolumes.IndexOf(element.DataContext as TriggerVolume);
        }

        public override SharpDX.BoundingBox GetObjectBounds(SceneNodeModel treeNode, int itemIndex)
        {
            return TriggerVolumes[itemIndex].GetTotalBounds();
        }

        public override IEnumerable<ScenarioListItem> GetListItems(SceneNodeModel treeNode)
        {
            return scenario.TriggerVolumes.Select(vol => new ScenarioListItem(vol.GetDisplayName(), vol));
        }

        internal override BlockPropertiesLocator GetPropertiesLocator(SceneNodeModel treeNode, int itemIndex)
        {
            if (itemIndex < 0)
                return null;

            var section = scenario.Sections[Section.TriggerVolumes];
            return new BlockPropertiesLocator
            {
                RootNode = section.Node,
                BaseAddress = section.TagBlock.Pointer.Address
                    + itemIndex * section.BlockSize,
                TargetObject = scenario.TriggerVolumes[itemIndex]
            };
        }

        public override bool ExecuteObjectOperation(SceneNodeModel treeNode, ObjectOperation operation, int itemIndex)
        {
            var blockRef = scenario.Sections[Section.TriggerVolumes];
            var blockEditor = scenario.MetadataStream.GetBlockEditor(blockRef.TagBlock.Pointer.Address);

            switch (operation)
            {
                case ObjectOperation.Add:
                    blockEditor.Add();

                    var vol = new TriggerVolume(scenario)
                    {   
                        //really should be reading these from the stream...
                        ForwardVector = new RealVector3D(1, 0, 0),
                        UpVector = new RealVector3D(0, 0, 1),
                        Size = new RealVector3D(1, 1, 1)
                    };

                    scenario.TriggerVolumes.Add(vol);
                    AddTriggerVolumeElement(vol);
                    break;

                case ObjectOperation.Remove:
                    if (itemIndex < 0 || itemIndex >= blockRef.TagBlock.Count)
                        return false;

                    blockEditor.Remove(itemIndex);
                    scenario.TriggerVolumes.RemoveAt(itemIndex);
                    RemoveTriggerVolumeElement(itemIndex);
                    break;

                case ObjectOperation.Copy:
                    if (itemIndex < 0 || itemIndex >= blockRef.TagBlock.Count)
                        return false;

                    var destIndex = itemIndex + 1;
                    blockEditor.Copy(itemIndex, destIndex);
                    vol = new TriggerVolume(scenario);
                    scenario.TriggerVolumes.Insert(destIndex, vol);
                    InsertTriggerVolumeElement(vol, destIndex);
                    vol.CopyFrom(scenario.TriggerVolumes[itemIndex]);
                    break;
            }

            blockEditor.UpdateBlockReference(blockRef);
            return true;
        }

        private void AddTriggerVolumeElement(TriggerVolume vol) => InsertTriggerVolumeElement(vol, TriggerVolumes.Count);

        private void InsertTriggerVolumeElement(TriggerVolume vol, int index)
        {
            var box = new BoxManipulator3D
            {
                DiffuseColor = TriggerVolume.DefaultColour,
                Position = ((IRealVector3D)vol.Position).ToVector3(),
                Size = ((IRealVector3D)vol.Size).ToVector3()
            };

            BindTriggerVolume(vol, box);

            TriggerVolumes.Insert(index, box);
            TriggerVolumeGroup.Children.Add(box);
        }

        private void RemoveTriggerVolumeElement(int index)
        {
            var element = TriggerVolumes[index];
            TriggerVolumes.Remove(element);
            TriggerVolumeGroup.Children.Remove(element);
            element.Dispose();
        }

        private void BindTriggerVolume(TriggerVolume vol, BoxManipulator3D box)
        {
            box.DataContext = vol;
            BindingOperations.SetBinding(box, BoxManipulator3D.ForwardVectorProperty,
                new Binding(nameof(TriggerVolume.ForwardVector)) { Mode = BindingMode.TwoWay, Converter = SharpDXVectorConverter.Instance });
            BindingOperations.SetBinding(box, BoxManipulator3D.UpVectorProperty,
                new Binding(nameof(TriggerVolume.UpVector)) { Mode = BindingMode.TwoWay, Converter = SharpDXVectorConverter.Instance });
            BindingOperations.SetBinding(box, BoxManipulator3D.PositionProperty,
                new Binding(nameof(TriggerVolume.Position)) { Mode = BindingMode.TwoWay, Converter = SharpDXVectorConverter.Instance });
            BindingOperations.SetBinding(box, BoxManipulator3D.SizeProperty,
                new Binding(nameof(TriggerVolume.Size)) { Mode = BindingMode.TwoWay, Converter = SharpDXVectorConverter.Instance });
        }

        public void RefreshTriggerVolumes(int selectedIndex)
        {
            for (int i = 0; i < sceneNode.Items.Count; i++)
            {
                var node = sceneNode.Items[i];
                (node.Tag as IMeshNode).SetVisibility(node.IsChecked == true || i == selectedIndex);
            }
        }

        public override void DisposeSceneElements()
        {
            TriggerVolumeGroup?.Dispose();
            TriggerVolumes.Clear();

            TriggerVolumeGroup = null;
            sceneNode.Items.Clear();
        }
    }
}
