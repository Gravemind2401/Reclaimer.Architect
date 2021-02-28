using Adjutant.Blam.Common;
using Reclaimer.Components;
using Reclaimer.Geometry;
using Reclaimer.Models;
using Reclaimer.Models.Ai;
using Reclaimer.Plugins;
using Reclaimer.Resources;
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
                case CacheType.MccHalo3U4:
                case CacheType.Halo3ODST:
                case CacheType.MccHalo3ODST:
                case CacheType.HaloReachRetail:
                case CacheType.MccHaloReach:
                case CacheType.MccHaloReachU3:
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

        private readonly ModelFactory factory = new ModelFactory();
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

        public void SelectPalette(SceneNodeModel node)
        {
            if (node == null || !isReady) return;

            foreach (var c in scenario.ComponentManagers)
                c.OnSelectedTreeNodeChanged(node);

            //remove selection highlighter if it is no longer visible
            var selection = renderer.GetSelectedElement();
            if (selection != null && (selection.IsRendering == false || selection.EnumerateAncestors().Any(e => e.IsRendering == false)))
                renderer.SetSelectedElement(null);
        }

        public void SelectObject(SceneNodeModel node, int itemIndex)
        {
            if (node == null || !isReady) return;

            if (itemIndex < 0)
                return;

            var handler = scenario.GetNodeTypeHandler(scenario.SelectedNodeType);
            renderer.SetSelectedElement(handler?.GetElement(node, itemIndex));
        }

        public void NavigateToObject(SceneNodeModel node, int index)
        {
            if (node == null || !isReady) return;

            var handler = scenario.GetNodeTypeHandler(scenario.SelectedNodeType);
            if (handler != null)
                renderer.ZoomToBounds(handler.GetObjectBounds(node, index), 500);
        }

        public void RefreshPalette(string paletteKey, int index)
        {
            renderer.SetSelectedElement(null);
            scenario.GetComponent<PaletteComponentManager>().RefreshPalette(factory, paletteKey, index);
        }

        public void RefreshObject(string paletteKey, ObjectPlacement placement, string fieldId)
        {
            renderer.SetSelectedElement(null);
            scenario.GetComponent<PaletteComponentManager>().RefreshObject(factory, paletteKey, placement, fieldId);
        }

        public void OnElementSelected(Helix.Element3D element)
        {
            if (element?.DataContext == null)
                return;

            var handler = scenario.GetNodeTypeHandler(scenario.SelectedNodeType);
            if (handler != null)
                scenario.SelectedItemIndex = handler.GetElementIndex(scenario.SelectedNode, element);
        }

        public void LoadScenario()
        {
            IsEnabled = false;
            spinner.Visibility = Visibility.Visible;

            Task.Run(async () =>
            {
                await Task.WhenAll(scenario.ComponentManagers.Select(c=> c.InitializeResourcesAsync(factory)));

                Dispatcher.Invoke(() =>
                {
                    PostLoadScenario();

                    isReady = true;
                    IsEnabled = true;
                    spinner.Visibility = Visibility.Collapsed;
                });
            });
        }

        private void PostLoadScenario()
        {
            foreach (var c in scenario.ComponentManagers)
                c.InitializeElements(factory);

            TreeViewItems.AddRange(scenario.ComponentManagers.SelectMany(c => c.GetSceneNodes()));
            modelGroup.Children.AddRange(scenario.ComponentManagers.SelectMany(c => c.GetSceneElements()));

            foreach (var c in scenario.ComponentManagers)
                c.OnSelectedTreeNodeChanged(scenario.SelectedNode);

            renderer.ScaleToContent();

            var elements = scenario.GetComponent<TerrainComponentManager>().BspElements;
            if (elements.Any())
            {
                var bounds = elements.GetTotalBounds();
                renderer.CameraSpeed = Math.Ceiling(bounds.Size.Length());
                renderer.ZoomToBounds(bounds);
            }

            modelGroup.Visibility = Visibility.Visible;
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

            scenario.GetComponent<TriggerVolumeComponentManager>().RefreshTriggerVolumes(scenario.SelectedItemIndex);
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

            foreach (var c in scenario.ComponentManagers)
                c.DisposeSceneElements();

            factory.Dispose();
            renderer.Dispose();
            GC.Collect();
        }
        #endregion
    }
}
