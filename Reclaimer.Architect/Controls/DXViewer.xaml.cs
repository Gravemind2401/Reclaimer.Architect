using Adjutant.Blam.Common;
using Adjutant.Geometry;
using Adjutant.Utilities;
using Microsoft.Win32;
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
using System.Windows.Input;
using System.Windows.Media.Animation;

using Helix = HelixToolkit.Wpf.SharpDX;

namespace Reclaimer.Controls
{
    /// <summary>
    /// Interaction logic for DXViewer.xaml
    /// </summary>
    public partial class DXViewer : IDisposable
    {
        private delegate IEnumerable<string> GetExportFormats();
        private delegate void WriteModelFile(IGeometryModel model, string fileName, string formatId);
        private delegate void ExportBitmaps(IRenderGeometry geometry);
        private delegate void ExportSelectedBitmaps(IRenderGeometry geometry, IEnumerable<int> shaderIndexes);

        private static readonly string[] DirectContentTags = new[] { "mode", "mod2", "sbsp" };

        private static readonly string[] AllLods = new[] { "Highest", "High", "Medium", "Low", "Lowest" };
        private static readonly Helix.Material ErrorMaterial = Helix.DiffuseMaterials.Gold;

        #region Dependency Properties
        private static readonly DependencyPropertyKey AvailableLodsPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(AvailableLods), typeof(IEnumerable<string>), typeof(DXViewer), new PropertyMetadata());

        public static readonly DependencyProperty AvailableLodsProperty = AvailableLodsPropertyKey.DependencyProperty;

        public static readonly DependencyProperty SelectedLodProperty =
            DependencyProperty.Register(nameof(SelectedLod), typeof(int), typeof(DXViewer), new PropertyMetadata(0, SelectedLodChanged));

        public static readonly DependencyProperty IsExportableProperty =
            DependencyProperty.Register(nameof(IsExportable), typeof(bool), typeof(DXViewer), new PropertyMetadata(false));

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

        public bool IsExportable
        {
            get { return (bool)GetValue(IsExportableProperty); }
            set { SetValue(IsExportableProperty, value); }
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

        public static bool CanOpenTag(IIndexItem tag) => ModelFactory.IsTagSupported(tag);

        private readonly Helix.GroupModel3D modelGroup = new Helix.GroupModel3D();

        private int modelId;
        private Helix.Element3D element;
        private ModelFactory modelFactory;
        private IRenderGeometry renderGeometry;

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

            renderGeometry = geometry;
            IsExportable = true;
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

            renderGeometry = null;
            IsExportable = false;
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

        private IEnumerable<IGeometryPermutation> GetSelectedPermutations(IGeometryModel model)
        {
            foreach (var parent in TreeViewItems.Where(i => i.IsChecked != false))
            {
                var region = model.Regions.ElementAtOrDefault((parent.Tag as RenderModel3D.Region)?.SourceIndex ?? -1);
                if (region == null)
                    continue;

                foreach (var child in parent.Items.Where(i => i.IsChecked == true))
                {
                    var permutation = region.Permutations.ElementAtOrDefault((child.Tag as RenderModel3D.Permutation)?.SourceIndex ?? -1);
                    if (permutation != null)
                        yield return permutation;
                }
            }
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
            SetState((e.OriginalSource as FrameworkElement).DataContext as TreeItemModel, true);
            isWorking = false;
        }

        private void SetState(TreeItemModel item, bool updateRender)
        {
            if (item.HasItems == false) //permutation
            {
                var parent = item.Parent as TreeItemModel;
                if (parent != null)
                {
                    var children = parent.Items.Where(i => i.IsVisible);

                    if (children.All(i => i.IsChecked == true))
                        parent.IsChecked = true;
                    else if (children.All(i => i.IsChecked == false))
                        parent.IsChecked = false;
                    else parent.IsChecked = null;
                }

                if (updateRender)
                    (item.Tag as IMeshNode)?.SetVisibility(item.IsChecked ?? false);
            }
            else //region
            {
                foreach (var i in item.Items.Where(i => i.IsVisible))
                {
                    i.IsChecked = item.IsChecked;

                    if (updateRender)
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

        #region Export Functions
        private bool PromptFileSave(IGeometryModel model, out string fileName, out string formatId)
        {
            var getExportFormats = Substrate.GetSharedFunction<GetExportFormats>("Reclaimer.Plugins.ModelViewerPlugin.GetExportFormats");

            var exportFormats = getExportFormats()
                .Select(f => new
                {
                    FormatId = f,
                    Extension = ModelViewerPlugin.GetFormatExtension(f),
                    Description = ModelViewerPlugin.GetFormatDescription(f)
                }).ToList();

            var filter = string.Join("|", exportFormats.Select(f => $"{f.Description}|*.{f.Extension}"));

            var sfd = new SaveFileDialog
            {
                OverwritePrompt = true,
                FileName = model.Name,
                Filter = filter,
                FilterIndex = 1 + exportFormats.TakeWhile(f => f.FormatId != ArchitectSettingsPlugin.Settings.DefaultSaveFormat).Count(),
                AddExtension = true
            };

            if (sfd.ShowDialog() != true)
            {
                fileName = formatId = null;
                return false;
            }

            fileName = sfd.FileName;
            formatId = exportFormats[sfd.FilterIndex - 1].FormatId;
            ArchitectSettingsPlugin.Settings.DefaultSaveFormat = formatId;
            return true;
        }

        private void btnExportAll_Click(object sender, RoutedEventArgs e)
        {
            using (var model = renderGeometry.ReadGeometry(SelectedLod))
            {
                var writeModelFile = Substrate.GetSharedFunction<WriteModelFile>("Reclaimer.Plugins.ModelViewerPlugin.WriteModelFile");

                string fileName, formatId;
                if (!PromptFileSave(model, out fileName, out formatId))
                    return;

                writeModelFile(model, fileName, formatId);
            }
        }

        private void btnExportSelected_Click(object sender, RoutedEventArgs e)
        {
            using (var model = renderGeometry.ReadGeometry(SelectedLod))
            {
                var writeModelFile = Substrate.GetSharedFunction<WriteModelFile>("Reclaimer.Plugins.ModelViewerPlugin.WriteModelFile");

                string fileName, formatId;
                if (!PromptFileSave(model, out fileName, out formatId))
                    return;

                var masked = new MaskedGeometryModel(model, GetSelectedPermutations(model));
                writeModelFile(masked, fileName, formatId);
            }
        }

        private void btnExportBitmaps_Click(object sender, RoutedEventArgs e)
        {
            var export = Substrate.GetSharedFunction<ExportBitmaps>("Reclaimer.Plugins.ModelViewerPlugin.ExportBitmaps");
            export?.Invoke(renderGeometry);
        }

        private void btnExportSelectedBitmaps_Click(object sender, RoutedEventArgs e)
        {
            using (var model = renderGeometry.ReadGeometry(SelectedLod))
            {
                var export = Substrate.GetSharedFunction<ExportSelectedBitmaps>("Reclaimer.Plugins.ModelViewerPlugin.ExportSelectedBitmaps");
                var matIndices = GetSelectedPermutations(model)
                    .SelectMany(p => Enumerable.Range(p.MeshIndex, p.MeshCount))
                    .Select(i => model.Meshes.ElementAtOrDefault(i))
                    .Where(m => m != null)
                    .SelectMany(m => m.Submeshes.Select(s => (int)s.MaterialIndex))
                    .Distinct()
                    .ToList();

                export.Invoke(renderGeometry, matIndices);
            }
        }
        #endregion
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

        private void ClearChildren()
        {
            element = null;
            foreach (var element in modelGroup.Children.ToList())
            {
                modelGroup.Children.Remove(element);
                element.Dispose();
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            TreeViewItems.Clear();
            ClearChildren();
            modelGroup.Dispose();
            renderer.Dispose();
            modelFactory.Dispose();
            GC.Collect();
        }
        #endregion
    }
}
