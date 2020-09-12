using Adjutant.Blam.Common;
using Reclaimer.Geometry;
using Reclaimer.Models;
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
    public partial class DXEditor : IScenarioRenderView, IDisposable
    {
        //private delegate bool GetDataFolder(out string dataFolder);
        //private delegate bool SaveImage(IBitmap bitmap, string baseDir);

        public static bool CanOpenTag(IIndexItem tag) => tag.ClassCode.ToLower() == "scnr";

        private readonly SceneManager sceneManager;
        private readonly Helix.GroupModel3D modelGroup = new Helix.GroupModel3D();

        private bool isReady;
        private ScenarioModel scenario;

        public TabModel TabModel { get; }
        public ObservableCollection<TreeItemModel> TreeViewItems { get; }

        public Action<string> LogOutput { get; set; }
        public Action<string, Exception> LogError { get; set; }
        public Action<string> SetStatus { get; set; }
        public Action ClearStatus { get; set; }

        public DXEditor()
        {
            InitializeComponent();
            TabModel = new TabModel(this, TabItemType.Document);
            TreeViewItems = new ObservableCollection<TreeItemModel>();
            sceneManager = new SceneManager();

            DataContext = this;

            modelGroup.Visibility = Visibility.Collapsed;
            renderer.AddChild(modelGroup);
        }

        public void ClearScenario()
        {
            scenario = null;
        }

        public void SetScenario(ScenarioModel scenario)
        {
            this.scenario = scenario;

            var fileName = scenario.ScenarioTag.FileName();
            TabModel.ToolTip = fileName;
            TabModel.Header = Utils.GetFileName(fileName);
        }
        
        public void SelectPalette(NodeType nodeType)
        {
            if (!isReady) return;

            sceneManager.TriggerVolumeGroup.IsRendering = nodeType == NodeType.TriggerVolumes;

            var paletteKey = PaletteType.FromNodeType(nodeType);
            if (paletteKey != null)
            {
                foreach (var palette in sceneManager.PaletteHolders.Values)
                    palette.GroupElement.IsHitTestVisible = palette.Name == paletteKey;
            }
        }

        public void SelectObject(NodeType nodeType, int itemIndex)
        {
            if (!isReady) return;

            SelectPalette(nodeType);

            var paletteKey = PaletteType.FromNodeType(nodeType);
            if (paletteKey != null && itemIndex >= 0)
            {
                var selected = sceneManager.PaletteHolders[paletteKey];
                renderer.SetSelectedElement(selected.Instances[itemIndex].Element);
            }
        }

        public void NavigateToObject(NodeType nodeType, int index)
        {
            if (!isReady) return;

            var paletteKey = PaletteType.FromNodeType(nodeType);
            if (paletteKey == null)
                return;

            var obj = sceneManager.PaletteHolders[paletteKey].Instances[index];
            if (obj != null)
                renderer.ZoomToBounds(obj.Element.GetTotalBounds(), 500);
        }

        public void LoadScenario()
        {
            Task.Run(() =>
            {
                sceneManager.ReadScenario(scenario);
                Dispatcher.Invoke(() =>
                {
                    sceneManager.RenderScenario();

                    foreach (var instance in sceneManager.BspHolder.Instances)
                    {
                        instance.Element.IsHitTestVisible = false;
                        modelGroup.Children.Add(instance.Element);
                    }

                    foreach (var instance in sceneManager.SkyHolder.Instances)
                    {
                        instance.Element.IsHitTestVisible = false;
                        modelGroup.Children.Add(instance.Element);
                    }

                    foreach (var holder in sceneManager.PaletteHolders.Values)
                    {
                        holder.GroupElement.IsHitTestVisible = false;
                        modelGroup.Children.Add(holder.GroupElement);
                    }

                    renderer.ScaleToContent();
                    var elements = sceneManager.BspHolder.Instances.Where(i => i != null)
                        .Select(i => i.Element);

                    if (elements.Any())
                    {
                        var bounds = elements.GetTotalBounds();
                        renderer.CameraSpeed = Math.Ceiling(bounds.Size.Length());
                        renderer.ZoomToBounds(bounds);
                    }

                    sceneManager.TriggerVolumeGroup.IsRendering = false;
                    modelGroup.Children.Add(sceneManager.TriggerVolumeGroup);

                    modelGroup.Visibility = Visibility.Visible;

                    #region Generate Tree Nodes
                    var bspNode = new TreeItemModel { Header = "sbsp", IsChecked = true };
                    for (int i = 0; i < sceneManager.BspHolder.Instances.Count; i++)
                    {
                        var bsp = sceneManager.BspHolder.Instances[i];
                        var tag = scenario.Bsps[i].Tag;
                        if (bsp == null)
                            continue;

                        var permNode = new TreeItemModel { Header = tag.FileName(), IsChecked = true, Tag = bsp };
                        bspNode.Items.Add(permNode);
                    }

                    if (bspNode.HasItems)
                        TreeViewItems.Add(bspNode);

                    var skyNode = new TreeItemModel { Header = "sky", IsChecked = true };
                    for (int i = 0; i < sceneManager.BspHolder.Instances.Count; i++)
                    {
                        var sky = sceneManager.SkyHolder.Instances[i];
                        var tag = scenario.Skies[i].Tag;
                        if (sky == null)
                            continue;

                        var permNode = new TreeItemModel { Header = tag.FileName(), IsChecked = true, Tag = sky };
                        skyNode.Items.Add(permNode);
                    }

                    if (skyNode.HasItems)
                        TreeViewItems.Add(skyNode);

                    foreach (var holder in sceneManager.PaletteHolders.Values)
                    {
                        var paletteNode = new TreeItemModel { Header = holder.Name, IsChecked = true };

                        foreach (var inst in holder.Instances.Where(i => i != null))
                        {
                            var permNode = new TreeItemModel { Header = inst.Name, IsChecked = true, Tag = inst };
                            paletteNode.Items.Add(permNode);
                        }

                        if (paletteNode.HasItems)
                            TreeViewItems.Add(paletteNode);
                    }
                    #endregion

                    isReady = true;
                });
            });
        }

        #region Treeview Events
        private void TreeViewItem_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as TreeItemModel;
            if (item != tv.SelectedItem)
                return; //because this event bubbles to the parent node

            var inst = item.Tag as IModelInstance;
            if (inst != null)
                renderer.ZoomToBounds(inst.Element.GetTotalBounds(), 500);
        }

        private bool isWorking = false;

        private void TreeViewItem_Checked(object sender, RoutedEventArgs e)
        {
            if (isWorking) return;

            isWorking = true;
            SetState((e.OriginalSource as FrameworkElement).DataContext as TreeItemModel);
            isWorking = false;
        }

        private void SetState(TreeItemModel item)
        {
            if (item.HasItems == false)
            {
                var parent = item.Parent as TreeItemModel;
                var children = parent.Items.Cast<TreeItemModel>();

                if (children.All(i => i.IsChecked == true))
                    parent.IsChecked = true;
                else if (children.All(i => i.IsChecked == false))
                    parent.IsChecked = false;
                else parent.IsChecked = null;

                if (item.Tag is IModelInstance)
                    (item.Tag as IModelInstance).Element.Visibility = (item.IsChecked ?? false) ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                foreach (TreeItemModel i in item.Items)
                {
                    i.IsChecked = item.IsChecked;
                    if (i.Tag is IModelInstance)
                        (i.Tag as IModelInstance).Element.Visibility = (i.IsChecked ?? false) ? Visibility.Visible : Visibility.Collapsed;
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

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            //var exportFormats = ModelViewerPlugin.GetExportFormats()
            //    .Select(f => new
            //    {
            //        FormatId = f,
            //        Extension = ModelViewerPlugin.GetFormatExtension(f),
            //        Description = ModelViewerPlugin.GetFormatDescription(f)
            //    }).ToList();

            //var filter = string.Join("|", exportFormats.Select(f => $"{f.Description}|*.{f.Extension}"));

            //var sfd = new SaveFileDialog
            //{
            //    OverwritePrompt = true,
            //    FileName = model.Name,
            //    Filter = filter,
            //    FilterIndex = 1 + exportFormats.TakeWhile(f => f.FormatId != ModelViewerPlugin.Settings.DefaultSaveFormat).Count(),
            //    AddExtension = true
            //};

            //if (sfd.ShowDialog() != true)
            //    return;

            //var option = exportFormats[sfd.FilterIndex - 1];

            //ModelViewerPlugin.WriteModelFile(model, sfd.FileName, option.FormatId);
            //ModelViewerPlugin.Settings.DefaultSaveFormat = option.FormatId;
        }
        #endregion

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

        #region IDisposable
        public void Dispose()
        {
            TreeViewItems.Clear();
            modelGroup.Children.Clear();

            sceneManager?.Dispose();
            renderer.Dispose();
            GC.Collect();
        }
        #endregion
    }
}
