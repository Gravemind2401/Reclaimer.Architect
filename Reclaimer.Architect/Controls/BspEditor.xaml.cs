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
    /// Interaction logic for BspEditor.xaml
    /// </summary>
    public partial class BspEditor : IStructureBspRenderView, IRendererHost, IDisposable
    {
        public static bool CanOpenTag(IIndexItem tag) => tag.ClassCode.ToLower() == "sbsp";

        #region Dependency Properties
        public static readonly DependencyProperty CanTranslateProperty =
            DependencyProperty.Register(nameof(CanTranslate), typeof(bool), typeof(BspEditor), new PropertyMetadata(true, ManipulationToggleChanged));

        public static readonly DependencyProperty CanRotateProperty =
            DependencyProperty.Register(nameof(CanRotate), typeof(bool), typeof(BspEditor), new PropertyMetadata(true, ManipulationToggleChanged));

        public static readonly DependencyProperty CanScaleProperty =
            DependencyProperty.Register(nameof(CanScale), typeof(bool), typeof(BspEditor), new PropertyMetadata(true, ManipulationToggleChanged));

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
            var editor = d as BspEditor;
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

        private readonly BspManager bspManager;
        private readonly Helix.GroupModel3D modelGroup = new Helix.GroupModel3D();

        private bool isReady;
        private StructureBspModel bspModel;

        public TabModel TabModel { get; }
        public ObservableCollection<TreeItemModel> TreeViewItems { get; }

        public BspEditor()
        {
            InitializeComponent();
            TabModel = new TabModel(this, TabItemType.Document);
            TreeViewItems = new ObservableCollection<TreeItemModel>();
            bspManager = new BspManager();

            DataContext = this;

            modelGroup.Visibility = Visibility.Collapsed;
            renderer.AddChild(modelGroup);

            CanTranslate = ArchitectSettingsPlugin.Settings.EditorTranslation;
            CanRotate = ArchitectSettingsPlugin.Settings.EditorRotation;
            CanScale = ArchitectSettingsPlugin.Settings.EditorScaling;
        }

        public void ClearScenario()
        {
            bspModel = null;
        }

        public void SetScenario(StructureBspModel bspModel)
        {
            this.bspModel = bspModel;

            var fileName = bspModel.StructureBspTag.FileName();
            TabModel.ToolTip = fileName;
            TabModel.Header = Utils.GetFileName(fileName);
        }

        //public void RefreshObject(string paletteKey, ObjectPlacement placement, string fieldId)
        //{
        //    renderer.SetSelectedElement(null);
        //    bspManager.RefreshObject(paletteKey, placement, fieldId);
        //}

        public void LoadStructureBsp()
        {
            IsEnabled = false;
            spinner.Visibility = Visibility.Visible;

            Task.Run(async () =>
            {
                await Task.WhenAll(bspManager.ReadStructureBsp(bspModel));

                Dispatcher.Invoke(() =>
                {
                    bspManager.RenderStructureBsp();

                    foreach (var instance in bspManager.ClusterHolder.Elements)
                    {
                        instance.IsHitTestVisible = false;
                        modelGroup.Children.Add(instance);
                    }

                    foreach (var holder in bspManager.InstanceHolders.Values)
                    {
                        //holder.GroupElement.IsHitTestVisible = false;
                        modelGroup.Children.Add(holder.GroupElement);
                    }

                    renderer.ScaleToContent();
                    var elements = bspManager.ClusterHolder.Elements.Where(i => i != null);

                    if (elements.Any())
                    {
                        var bounds = elements.GetTotalBounds();
                        renderer.CameraSpeed = Math.Ceiling(bounds.Size.Length());
                        renderer.ZoomToBounds(bounds);
                    }

                    modelGroup.Visibility = Visibility.Visible;

                    #region Generate Tree Nodes
                    var bspNode = new TreeItemModel { Header = bspManager.ClusterHolder.Name, IsChecked = true };
                    for (int i = 0; i < bspManager.ClusterHolder.Elements.Count; i++)
                    {
                        var bsp = bspManager.ClusterHolder.Elements[i];
                        var permNode = new TreeItemModel { Header = $"Cluster {i:D3}", IsChecked = true, Tag = bsp };
                        bspNode.Items.Add(permNode);
                    }

                    if (bspNode.HasItems)
                        TreeViewItems.Add(bspNode);

                    foreach (var holder in bspManager.InstanceHolders.Values)
                    {
                        var paletteNode = new TreeItemModel { Header = holder.Name, IsChecked = true };

                        for (int i = 0; i < holder.Elements.Count; i++)
                        {
                            //var info = holder.GetInfoForIndex(i);

                            //var permNode = info.TreeItem = new TreeItemModel { Header = info.Placement.GetDisplayName(), IsChecked = true, Tag = info.Element };
                            var permNode = new TreeItemModel { Header = holder.Placements[i].Name, IsChecked = true, Tag = holder.Elements[i] };
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

        public void OnElementSelected(Helix.Element3D element)
        {
            bspModel.PropertyView.CurrentItem = element?.DataContext as InstancePlacement;
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
            var obj = selection?.DataContext as InstancePlacement;
            if (obj == null)
                return;

            var camPos = renderer.Viewport.Camera.Position;
            obj.M41 = (float)camPos.X;
            obj.M42 = (float)camPos.Y;
            obj.M43 = (float)camPos.Z;
        }
        #endregion

        #region Control Events
        private void txtSearch_SearchChanged(object sender, RoutedEventArgs e)
        {
            foreach (var parent in TreeViewItems)
            {
                foreach (var child in parent.Items)
                {
                    var match = string.IsNullOrEmpty(txtSearch.Text) || child.Header.ToUpperInvariant().Contains(txtSearch.Text.ToUpperInvariant());
                    child.Visibility = match ? Visibility.Visible : Visibility.Collapsed;

                    var element = child.Tag as Helix.Element3D;
                    if (element?.DataContext is InstancePlacement)
                        element.IsHitTestVisible = match;
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
            bspManager.Dispose();
            renderer.Dispose();
            GC.Collect();
        }
        #endregion
    }
}
