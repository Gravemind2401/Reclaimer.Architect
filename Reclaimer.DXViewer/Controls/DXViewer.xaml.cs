using Adjutant.Blam.Common;
using Adjutant.Geometry;
using Adjutant.Utilities;
using Reclaimer.Geometry;
using Reclaimer.Models;
using Reclaimer.Utilities;
using Studio.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Dds;
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

using Helix = HelixToolkit.Wpf.SharpDX;

namespace Reclaimer.Controls
{
    /// <summary>
    /// Interaction logic for DXViewer.xaml
    /// </summary>
    public partial class DXViewer : IDisposable
    {
        //private delegate bool GetDataFolder(out string dataFolder);
        //private delegate bool SaveImage(IBitmap bitmap, string baseDir);

        private static readonly string[] DirectContentTags = new[] { "mode", "mod2", "sbsp" };

        private static readonly string[] AllLods = new[] { "Highest", "High", "Medium", "Low", "Lowest" };
        private static readonly Helix.Material ErrorMaterial = Helix.DiffuseMaterials.Gold;

        #region Dependency Properties
        private static readonly DependencyPropertyKey AvailableLodsPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(AvailableLods), typeof(IEnumerable<string>), typeof(DXViewer), new PropertyMetadata());

        public static readonly DependencyProperty AvailableLodsProperty = AvailableLodsPropertyKey.DependencyProperty;

        public static readonly DependencyProperty SelectedLodProperty =
            DependencyProperty.Register(nameof(SelectedLod), typeof(int), typeof(DXViewer), new PropertyMetadata(0, SelectedLodChanged));

        public IEnumerable<string> AvailableLods
        {
            get { return (IEnumerable<string>)GetValue(AvailableLodsProperty); }
            private set { SetValue(AvailableLodsPropertyKey, value); }
        }

        public int SelectedLod
        {
            get { return (int)GetValue(SelectedLodProperty); }
            set { SetValue(SelectedLodProperty, value); }
        }

        public static void SelectedLodChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DXViewer)d;
            if (control.renderModel != null)
                control.SetLod((int)e.NewValue);
            else if (control.objectModel != null)
                control.SetVariant((int)e.NewValue);
        }
        #endregion

        public static bool CanOpenTag(IIndexItem tag) => DirectContentTags.Any(t => tag.ClassCode.ToLower() == t) || CompositeModelFactory.IsTagSupported(tag);

        private readonly Helix.GroupModel3D modelGroup = new Helix.GroupModel3D();

        private int modelId;
        private Helix.Element3D element;
        private ModelFactory modelFactory;

        private RenderModel3D renderModel => element as RenderModel3D;
        private ObjectModel3D objectModel => element as ObjectModel3D;

        public TabModel TabModel { get; }
        public ObservableCollection<TreeItemModel> TreeViewItems { get; }

        public Action<string> LogOutput { get; set; }
        public Action<string, Exception> LogError { get; set; }
        public Action<string> SetStatus { get; set; }
        public Action ClearStatus { get; set; }

        public DXViewer()
        {
            InitializeComponent();
            TabModel = new TabModel(this, TabItemType.Document);
            TreeViewItems = new ObservableCollection<TreeItemModel>();
            modelFactory = new ModelFactory();

            DataContext = this;

            modelGroup.IsHitTestVisible = false;
            renderer.AddChild(modelGroup);
        }

        public void LoadGeometry(IRenderGeometry geometry, string fileName)
        {
            TabModel.ToolTip = fileName;
            TabModel.Header = Utils.GetFileName(fileName);

            ClearChildren();
            modelId = geometry.Id;
            modelFactory.LoadGeometry(geometry, true);

            AvailableLods = AllLods.Take(geometry.LodCount);
            SetLod(0);
        }

        public void LoadGeometry(IIndexItem modelTag, string fileName)
        {
            if (!CanOpenTag(modelTag))
                throw new NotSupportedException($"{modelTag.ClassName} tags are not supported.");

            if (DirectContentTags.Any(t => modelTag.ClassCode.ToLower() == t))
            {
                IRenderGeometry geometry;
                if (ContentFactory.TryGetGeometryContent(modelTag, out geometry))
                {
                    LoadGeometry(geometry, fileName);
                    return;
                }
                else throw new ArgumentException($"Could not load geometry from tag", nameof(modelTag));
            }

            TabModel.ToolTip = fileName;
            TabModel.Header = Utils.GetFileName(fileName);

            ClearChildren();
            modelId = modelTag.Id;
            modelFactory.LoadTag(modelTag, false);

            element = modelFactory.CreateObjectModel(modelId);
            AvailableLods = objectModel.Variants;
            modelGroup.Children.Add(element);
            SetVariant(Math.Max(0, AvailableLods.ToList().IndexOf(objectModel.DefaultVariant)));
        }

        private void SetLod(int index)
        {
            TreeViewItems.Clear();
            ClearChildren();

            element = modelFactory.CreateRenderModel(modelId, index);
            modelGroup.Children.Add(element);

            AddRenderModelNodes(renderModel.Regions, r => r.Permutations);
            AddRenderModelNodes(renderModel.InstanceGroups, g => g.Instances);
        }

        private void AddRenderModelNodes<TParent, TChild>(IEnumerable<TParent> collection, Func<TParent, IEnumerable<TChild>> getChildren) 
            where TParent : IMeshNode 
            where TChild : IMeshNode
        {
            foreach (var parent in collection)
            {
                var parentNode = new TreeItemModel { Header = parent.Name, IsChecked = true, Tag = parent };

                foreach (var child in getChildren(parent))
                {
                    var childNode = new TreeItemModel { Header = child.Name, IsChecked = true, Tag = child };
                    parentNode.Items.Add(childNode);
                }

                if (parentNode.HasItems)
                    TreeViewItems.Add(parentNode);
            }
        }

        private void SetVariant(int index)
        {
            TreeViewItems.Clear();

            var variantName = AvailableLods.Skip(index).FirstOrDefault();
            objectModel.SetVariant(variantName);

            foreach (var child in objectModel.Children.OfType<IMeshNode>())
                TreeViewItems.Add(new TreeItemModel { Header = child.Name, IsChecked = true, Tag = child });
        }

        #region Treeview Events
        private void TreeViewItem_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as TreeItemModel;
            if (item != tv.SelectedItem)
                return; //because this event bubbles to the parent node

            var node = item.Tag as IMeshNode;
            if (node != null)
                renderer.ZoomToBounds(node.GetNodeBounds(), 500);
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
                if (parent != null)
                {
                    var children = parent.Items.Cast<TreeItemModel>();

                    if (children.All(i => i.IsChecked == true))
                        parent.IsChecked = true;
                    else if (children.All(i => i.IsChecked == false))
                        parent.IsChecked = false;
                    else parent.IsChecked = null;
                }

                (item.Tag as IMeshNode)?.SetVisibility(item.IsChecked ?? false);
            }
            else
            {
                foreach (TreeItemModel i in item.Items)
                {
                    i.IsChecked = item.IsChecked;
                    (i.Tag as IMeshNode)?.SetVisibility(i.IsChecked ?? false);
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

        private void btnSaveBitmaps_Click(object sender, RoutedEventArgs e)
        {
            //var getFolder = Substrate.GetSharedFunction<GetDataFolder>("Reclaimer.Plugins.BatchExtractPlugin.GetDataFolder");

            //string folder;
            //if (!getFolder(out folder))
            //    return;

            //Task.Run(() =>
            //{
            //    var saveImage = Substrate.GetSharedFunction<SaveImage>("Reclaimer.Plugins.BatchExtractPlugin.SaveImage");

            //    foreach (var bitm in geometry.GetAllBitmaps())
            //    {
            //        try
            //        {
            //            SetStatus($"Extracting {bitm.Name}");
            //            saveImage(bitm, folder);
            //            LogOutput($"Extracted {bitm.Name}.{bitm.Class}");
            //        }
            //        catch (Exception ex)
            //        {
            //            LogError($"Error extracting {bitm.Name}.{bitm.Class}", ex);
            //        }
            //    }

            //    ClearStatus();
            //    LogOutput($"Recursive bitmap extract complete for {geometry.Name}.{geometry.Class}");
            //});
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

        private void ClearChildren()
        {
            element = null;
            foreach (var element in modelGroup.Children.ToList())
            {
                modelGroup.Children.Remove(element);
                element.Dispose();
            }
        }

        #region IDisposable
        public void Dispose()
        {
            TreeViewItems.Clear();
            ClearChildren();
            modelGroup.Dispose();
            renderer.Dispose();
            GC.Collect();
        }
        #endregion
    }
}
