using Adjutant.Blam.Common;
using Reclaimer.Geometry;
using Reclaimer.Models;
using Reclaimer.Plugins;
using Reclaimer.Utilities;
using Studio.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

using static HelixToolkit.Wpf.SharpDX.Media3DExtension;

using Helix = HelixToolkit.Wpf.SharpDX;

namespace Reclaimer.Controls
{
    /// <summary>
    /// Interaction logic for DXViewer.xaml
    /// </summary>
    public partial class DXEditor : IScenarioRenderView, IRendererHost, IDisposable
    {
        public static bool CanOpenTag(IIndexItem tag)
        {
            if (tag.ClassCode.ToLower() != "scnr")
                return false;

            switch (tag.CacheFile.CacheType)
            {
                case CacheType.Halo3Retail:
                case CacheType.MccHalo3:
                case CacheType.Halo3ODST:
                case CacheType.MccHalo3ODST:
                    return true;
                default: return false;
            }
        }

        #region Dependency Properties
        public static readonly DependencyProperty CanTranslateProperty =
            DependencyProperty.Register(nameof(CanTranslate), typeof(bool), typeof(DXEditor), new PropertyMetadata(true, ManipulationToggleChanged));

        public static readonly DependencyProperty CanRotateProperty =
            DependencyProperty.Register(nameof(CanRotate), typeof(bool), typeof(DXEditor), new PropertyMetadata(true, ManipulationToggleChanged));

        public static readonly DependencyProperty CanScaleProperty =
            DependencyProperty.Register(nameof(CanScale), typeof(bool), typeof(DXEditor), new PropertyMetadata(true, ManipulationToggleChanged));

        public bool CanTranslate
        {
            get { return (bool)GetValue(CanTranslateProperty); }
            set { SetValue(CanTranslateProperty, value); }
        }

        public bool CanRotate
        {
            get { return (bool)GetValue(CanRotateProperty); }
            set { SetValue(CanRotateProperty, value); }
        }

        public bool CanScale
        {
            get { return (bool)GetValue(CanScaleProperty); }
            set { SetValue(CanScaleProperty, value); }
        }

        private static void ManipulationToggleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editor = d as DXEditor;
            var flags = ManipulationFlags.None;

            if (editor.CanTranslate) flags |= ManipulationFlags.Translate;
            if (editor.CanRotate) flags |= ManipulationFlags.Rotate;
            if (editor.CanScale) flags |= ManipulationFlags.Scale;

            editor.renderer.ManipulationFlags = flags;

            ArchitectSettingsPlugin.Settings.EditorTranslation = editor.CanTranslate;
            ArchitectSettingsPlugin.Settings.EditorRotation = editor.CanRotate;
            ArchitectSettingsPlugin.Settings.EditorScaling = editor.CanScale;
        }
        #endregion

        private readonly SceneManager sceneManager;
        private readonly Helix.GroupModel3D modelGroup = new Helix.GroupModel3D();

        private bool isReady;
        private ScenarioModel scenario;

        public TabModel TabModel { get; }
        public ObservableCollection<TreeItemModel> TreeViewItems { get; }

        public DXEditor()
        {
            InitializeComponent();
            TabModel = new TabModel(this, TabItemType.Document);
            TreeViewItems = new ObservableCollection<TreeItemModel>();
            sceneManager = new SceneManager();

            DataContext = this;

            modelGroup.Visibility = Visibility.Collapsed;
            renderer.AddChild(modelGroup);

            CanTranslate = ArchitectSettingsPlugin.Settings.EditorTranslation;
            CanRotate = ArchitectSettingsPlugin.Settings.EditorRotation;
            CanScale = ArchitectSettingsPlugin.Settings.EditorScaling;
        }

        public void ClearScenario()
        {
            scenario = null;
        }

        public void SetScenario(ScenarioModel scenario)
        {
            this.scenario = scenario;

            var fileName = $"{scenario.ScenarioTag.FileName()}.{scenario.ScenarioTag.ClassName}";
            TabModel.ToolTip = fileName;
            TabModel.Header = Utils.GetFileName(fileName);
        }

        public void SelectPalette(NodeType nodeType)
        {
            if (!isReady) return;

            sceneManager.StartPositionGroup.IsRendering = nodeType == NodeType.StartPositions;
            sceneManager.TriggerVolumeGroup.IsRendering = nodeType == NodeType.TriggerVolumes;

            //if not a palette this will disable hit testing on all palettes
            var paletteKey = PaletteType.FromNodeType(nodeType);
            foreach (var palette in sceneManager.PaletteHolders.Values)
                palette.GroupElement.IsHitTestVisible = palette.Name == paletteKey;
        }

        public void SelectObject(NodeType nodeType, int itemIndex)
        {
            if (!isReady) return;

            SelectPalette(nodeType);

            var paletteKey = PaletteType.FromNodeType(nodeType);
            if (paletteKey != null && itemIndex >= 0)
            {
                var selected = sceneManager.PaletteHolders[paletteKey];
                renderer.SetSelectedElement(selected.Elements[itemIndex]);
            }
            else if (nodeType == NodeType.StartPositions && itemIndex >= 0)
            {
                renderer.SetSelectedElement(sceneManager.StartPositions[itemIndex]);
            }
            else if (nodeType == NodeType.TriggerVolumes && itemIndex >= 0)
            {
                var prev = sceneManager.TriggerVolumes.FirstOrDefault(t => t.Tag != null);
                if (prev != null)
                {
                    prev.DiffuseColor = TriggerVolume.DefaultColour;
                    prev.Tag = null;
                }

                var selected = sceneManager.TriggerVolumes[itemIndex];
                selected.DiffuseColor = TriggerVolume.SelectedColour;
                selected.Tag = true;
            }
        }

        public void NavigateToObject(NodeType nodeType, int index)
        {
            if (!isReady) return;

            var paletteKey = PaletteType.FromNodeType(nodeType);
            if (paletteKey != null)
            {
                var obj = sceneManager.PaletteHolders[paletteKey].Elements[index] as IMeshNode;
                if (obj != null)
                    renderer.ZoomToBounds(obj.GetNodeBounds(), 500);
            }
            else if (nodeType == NodeType.StartPositions)
            {
                var obj = sceneManager.StartPositions[index];
                renderer.ZoomToBounds(obj.GetTotalBounds(), 500);
            }
            else if (nodeType == NodeType.TriggerVolumes)
            {
                var obj = sceneManager.TriggerVolumes[index];
                renderer.ZoomToBounds(new SharpDX.BoundingBox(obj.Position, obj.Position + obj.Size), 500);
            }
        }

        public void RefreshPalette(string paletteKey, int index)
        {
            renderer.SetSelectedElement(null);
            sceneManager.RefreshPalette(paletteKey, index);
        }

        public void RefreshObject(string paletteKey, ObjectPlacement placement, string fieldId)
        {
            renderer.SetSelectedElement(null);
            sceneManager.RefreshObject(paletteKey, placement, fieldId);
        }

        public void OnElementSelected(Helix.Element3D element)
        {
            if (element?.DataContext == null)
                return;

            var nodeType = (NodeType)scenario.SelectedNode.Tag;

            if (nodeType == NodeType.StartPositions)
                scenario.SelectedItemIndex = scenario.StartingPositions.IndexOf(element.DataContext as StartPosition);
            else if (nodeType == NodeType.TriggerVolumes)
            {

            }
            else
            {
                var paletteKey = PaletteType.FromNodeType(nodeType);
                scenario.SelectedItemIndex = scenario.Palettes[paletteKey].Placements.IndexOf(element.DataContext as ObjectPlacement);
            }
        }

        public void LoadScenario()
        {
            IsEnabled = false;
            spinner.Visibility = Visibility.Visible;

            Task.Run(async () =>
            {
                await Task.WhenAll(sceneManager.ReadScenario(scenario));

                Dispatcher.Invoke(() =>
                {
                    sceneManager.RenderScenario();

                    foreach (var instance in sceneManager.BspHolder.Elements)
                    {
                        instance.IsHitTestVisible = false;
                        modelGroup.Children.Add(instance);
                    }

                    foreach (var instance in sceneManager.SkyHolder.Elements)
                    {
                        instance.IsHitTestVisible = false;
                        modelGroup.Children.Add(instance);
                    }

                    foreach (var holder in sceneManager.PaletteHolders.Values)
                    {
                        holder.GroupElement.IsHitTestVisible = false;
                        modelGroup.Children.Add(holder.GroupElement);
                    }

                    renderer.ScaleToContent();
                    var elements = sceneManager.BspHolder.Elements.Where(i => i != null);

                    if (elements.Any())
                    {
                        var bounds = elements.GetTotalBounds();
                        renderer.CameraSpeed = Math.Ceiling(bounds.Size.Length());
                        renderer.ZoomToBounds(bounds);
                    }

                    sceneManager.StartPositionGroup.IsRendering = false;
                    sceneManager.TriggerVolumeGroup.IsRendering = false;

                    modelGroup.Children.Add(sceneManager.StartPositionGroup);
                    modelGroup.Children.Add(sceneManager.TriggerVolumeGroup);

                    modelGroup.Visibility = Visibility.Visible;

                    #region Generate Tree Nodes
                    var bspNode = new TreeItemModel { Header = sceneManager.BspHolder.Name, IsChecked = true };
                    for (int i = 0; i < sceneManager.BspHolder.Elements.Count; i++)
                    {
                        var bsp = sceneManager.BspHolder.Elements[i];
                        var tag = scenario.Bsps[i].Tag;
                        if (bsp == null)
                            continue;

                        var permNode = new TreeItemModel { Header = tag?.FileName() ?? "<null>", IsChecked = true, Tag = bsp };
                        bspNode.Items.Add(permNode);
                    }

                    if (bspNode.HasItems)
                        TreeViewItems.Add(bspNode);

                    var skyNode = new TreeItemModel { Header = sceneManager.SkyHolder.Name, IsChecked = true };
                    for (int i = 0; i < sceneManager.SkyHolder.Elements.Count; i++)
                    {
                        var sky = sceneManager.SkyHolder.Elements[i];
                        var tag = scenario.Skies[i].Tag;
                        if (sky == null)
                            continue;

                        var permNode = new TreeItemModel { Header = tag?.FileName() ?? "<null>", IsChecked = true, Tag = sky };
                        skyNode.Items.Add(permNode);
                    }

                    if (skyNode.HasItems)
                        TreeViewItems.Add(skyNode);

                    foreach (var holder in sceneManager.PaletteHolders.Values)
                    {
                        var paletteNode = new TreeItemModel { Header = holder.Name, IsChecked = true };

                        for (int i = 0; i < holder.Elements.Count; i++)
                        {
                            var info = holder.GetInfoForIndex(i);

                            var permNode = info.TreeItem = new TreeItemModel { Header = info.Placement.GetDisplayName(), IsChecked = true, Tag = info.Element };
                            paletteNode.Items.Add(permNode);
                        }

                        if (paletteNode.HasItems)
                            TreeViewItems.Add(paletteNode);
                    }
                    #endregion

                    isReady = true;
                    IsEnabled = true;
                    spinner.Visibility = Visibility.Collapsed;
                });
            });
        }

        #region Treeview Events
        private void TreeViewItem_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as TreeItemModel;
            if (item != tv.SelectedItem)
                return; //because this event bubbles to the parent node

            var node = item.Tag as IMeshNode;
            if (node != null)
                renderer.ZoomToBounds(node.GetNodeBounds());
        }

        private bool isWorking = false;

        private void TreeViewItem_Checked(object sender, RoutedEventArgs e)
        {
            if (isWorking) return;

            isWorking = true;
            SetState((e.OriginalSource as FrameworkElement).DataContext as TreeItemModel, true);
            isWorking = false;
        }

        private void SetState(TreeItemModel item, bool updateRender)
        {
            if (item.HasItems == false)
            {
                var parent = item.Parent as TreeItemModel;
                var children = parent.Items.Where(i => i.IsVisible);

                if (children.All(i => i.IsChecked == true))
                    parent.IsChecked = true;
                else if (children.All(i => i.IsChecked == false))
                    parent.IsChecked = false;
                else parent.IsChecked = null;

                if (updateRender)
                    (item.Tag as IMeshNode)?.SetVisibility(item.IsChecked ?? false);
            }
            else
            {
                foreach (var i in item.Items.Where(i => i.IsVisible))
                {
                    i.IsChecked = item.IsChecked;

                    if (updateRender)
                        (i.Tag as IMeshNode)?.SetVisibility(item.IsChecked ?? false);
                }
            }
        }
        #endregion

        #region Toolbar Events
        private void btnCollapseAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in TreeViewItems)
                item.IsExpanded = false;
        }

        private void btnExpandAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in TreeViewItems)
                item.IsExpanded = true;
        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in TreeViewItems)
                item.IsChecked = true;
        }

        private void btnSelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in TreeViewItems)
                item.IsChecked = false;
        }

        private void btnBringToCamera_Click(object sender, RoutedEventArgs e)
        {
            var selection = renderer.GetSelectedElement();
            var obj = selection?.DataContext as ScenarioObject;
            if (obj == null)
                return;

            obj.Position = renderer.Viewport.Camera.Position.ToRealVector3D();
        }
        #endregion

        #region Control Events
        private void txtSearch_SearchChanged(object sender, RoutedEventArgs e)
        {
            foreach (var parent in TreeViewItems)
            {
                foreach (var child in parent.Items)
                {
                    child.Visibility = string.IsNullOrEmpty(txtSearch.Text) || child.Header.ToUpperInvariant().Contains(txtSearch.Text.ToUpperInvariant())
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }

                parent.Visibility = parent.Items.Any(i => i.IsVisible)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            isWorking = true;
            foreach (var item in TreeViewItems)
                SetState(item.Items.First(), false);
            isWorking = false;
        }

        private void btnToggleDetails_Click(object sender, RoutedEventArgs e)
        {
            var toggle = sender as ToggleButton;
            if (toggle.IsChecked == true)
            {
                toggle.Tag = SplitPanel.GetDesiredSize(splitPanel.Children[0]);
                SplitPanel.SetDesiredSize(splitPanel.Children[0], new GridLength(0));
                splitPanel.SplitterThickness = 0;
            }
            else
            {
                SplitPanel.SetDesiredSize(splitPanel.Children[0], (GridLength)toggle.Tag);
                splitPanel.SplitterThickness = 3;
            }
        }

        private void PosLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var label = sender as Label;
            if (label == null)
                return;

            var anim = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(1)
            };

            Clipboard.SetText(label.Content?.ToString());
            label.BeginAnimation(OpacityProperty, anim);
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            TreeViewItems.Clear();
            modelGroup.Children.Clear();
            sceneManager.Dispose();
            renderer.Dispose();
            GC.Collect();
        }
        #endregion
    }
}
