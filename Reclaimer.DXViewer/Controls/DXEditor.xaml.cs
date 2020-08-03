using Adjutant.Blam.Common;
using Adjutant.Geometry;
using Adjutant.Spatial;
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

using static HelixToolkit.Wpf.SharpDX.Media3DExtension;

using Helix = HelixToolkit.Wpf.SharpDX;

namespace Reclaimer.Controls
{
    /// <summary>
    /// Interaction logic for DXViewer.xaml
    /// </summary>
    public partial class DXEditor : IDisposable
    {
        //private delegate bool GetDataFolder(out string dataFolder);
        //private delegate bool SaveImage(IBitmap bitmap, string baseDir);

        public static bool CanOpenTag(IIndexItem tag) => tag.ClassCode.ToLower() == "scnr";

        private readonly SceneManager sceneManager;
        private readonly Helix.GroupModel3D modelGroup = new Helix.GroupModel3D();

        private Blam.Halo3.scenario scenario;

        private readonly List<ModelManager> bspPalette;
        private readonly List<CompositeModelManager> sceneryPalette;
        private readonly List<CompositeModelManager> bipedPalette;
        private readonly List<CompositeModelManager> vehiclePalette;
        private readonly List<CompositeModelManager> equipmentPalette;
        private readonly List<CompositeModelManager> weaponPalette;
        private readonly List<CompositeModelManager> cratePalette;

        private readonly List<ModelInstance> bspInstances;
        private readonly Dictionary<string, List<CompositeModelInstance>> objectInstances;

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

            bspPalette = new List<ModelManager>();
            sceneryPalette = new List<CompositeModelManager>();
            bipedPalette = new List<CompositeModelManager>();
            vehiclePalette = new List<CompositeModelManager>();
            equipmentPalette = new List<CompositeModelManager>();
            weaponPalette = new List<CompositeModelManager>();
            cratePalette = new List<CompositeModelManager>();

            bspInstances = new List<ModelInstance>();
            objectInstances = new Dictionary<string, List<CompositeModelInstance>>();

            DataContext = this;

            renderer.AddChild(modelGroup);
        }

        public void LoadGeometry(IIndexItem scenarioTag, string fileName)
        {
            if (!CanOpenTag(scenarioTag))
                throw new NotSupportedException($"{scenarioTag.ClassName} tags are not supported.");

            TabModel.ToolTip = fileName;
            TabModel.Header = Utils.GetFileName(fileName);

            scenario = scenarioTag.ReadMetadata<Blam.Halo3.scenario>();

            foreach (var bsp in scenario.StructureBsps)
            {
                IRenderGeometry geom;
                if (ContentFactory.TryGetGeometryContent(bsp.BspReference.Tag, out geom))
                    bspPalette.Add(new ModelManager(sceneManager, geom.ReadGeometry(0)));
                else bspPalette.Add(null);
            }

            LoadPalette(scenario.SceneryPalette, sceneryPalette);
            //LoadPalette(scenario.BipedPalette, bipedPalette);
            //LoadPalette(scenario.VehiclePalette, vehiclePalette);
            //LoadPalette(scenario.WeaponPalette, weaponPalette);
            //LoadPalette(scenario.EquipmentPalette, equipmentPalette);
            //LoadPalette(scenario.CratePalette, cratePalette);

            var bspNode = new TreeItemModel { Header = "sbsp", IsChecked = true };

            for (int i = 0; i < scenario.StructureBsps.Count; i++)
            {
                var bsp = scenario.StructureBsps[i];
                var manager = bspPalette[i];

                if (manager == null)
                {
                    bspInstances.Add(null);
                    continue;
                }

                var inst = manager.GenerateModel();
                bspInstances.Add(inst);

                var permNode = new TreeItemModel { Header = bsp.BspReference.Tag.FileName(), IsChecked = true, Tag = inst };
                bspNode.Items.Add(permNode);
            }

            if (bspNode.HasItems)
                TreeViewItems.Add(bspNode);

            LoadInstances("scen", scenario.SceneryPlacements, sceneryPalette);
            //LoadInstances("bipd", scenario.BipedPlacements, bipedPalette);
            //LoadInstances("vehi", scenario.VehiclePlacements, vehiclePalette);
            //LoadInstances("weap", scenario.WeaponPlacements, weaponPalette);
            //LoadInstances("eqip", scenario.EquipmentPlacements, equipmentPalette);
            //LoadInstances("bloc", scenario.CratePlacements, cratePalette);

            foreach (var m in bspInstances.Where(i => i != null))
                modelGroup.Children.Add(m.Element);

            foreach (var m in objectInstances.Values.SelectMany(x => x).Where(i => i != null))
                modelGroup.Children.Add(m.Element);
        }

        private void LoadPalette(IEnumerable<Blam.Halo3.PaletteItem> items, IList<CompositeModelManager> target)
        {
            foreach (var item in items)
            {
                CompositeGeometryModel geom;
                if (CompositeModelFactory.TryGetModel(item.ObjectReference.Tag, out geom))
                    target.Add(new CompositeModelManager(sceneManager, geom));
                else target.Add(null);
            }
        }

        private void LoadInstances(string name, IEnumerable<Blam.Halo3.ObjectPlacement> items, IList<CompositeModelManager> palette)
        {
            var instances = new List<CompositeModelInstance>();
            objectInstances.Add(name, instances);

            var regNode = new TreeItemModel { Header = name, IsChecked = true };

            foreach (var item in items)
            {
                var manager = item.PaletteIndex >= 0 ? palette[item.PaletteIndex] : null;
                if (manager == null)
                {
                    instances.Add(null);
                    continue;
                }

                var inst = manager.GenerateModel();
                instances.Add(inst);

                var mat = SharpDX.Matrix.Scaling(item.Scale)
                    * SharpDX.Matrix.RotationYawPitchRoll(item.Rotation.Z, item.Rotation.Y, item.Rotation.X)
                    * SharpDX.Matrix.Translation(((IRealVector3D)item.Position).ToVector3());

                //var mat = SharpDX.Matrix.Scaling(item.Scale)
                //    * SharpDX.Matrix.RotationX(item.Rotation.X)
                //    * SharpDX.Matrix.RotationY(item.Rotation.Y)
                //    * SharpDX.Matrix.RotationZ(item.Rotation.Z)
                //    * SharpDX.Matrix.Translation(((IRealVector3D)item.Position).ToVector3());

                inst.Element.Transform = new MatrixTransform3D(mat.ToMatrix3D());

                var itemName = item.NameIndex >= 0 ? scenario.ObjectNames[item.NameIndex].Name : "unnamed";
                var permNode = new TreeItemModel { Header = itemName, IsChecked = true, Tag = inst };
                regNode.Items.Add(permNode);
            }

            if (regNode.HasItems)
                TreeViewItems.Add(regNode);
        }

        #region Treeview Events
        private void TreeViewItem_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as TreeItemModel;
            if (item != tv.SelectedItem)
                return; //because this event bubbles to the parent node

            var inst = item.Tag as CompositeModelInstance;
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

                if (item.Tag is ModelInstance)
                    (item.Tag as ModelInstance).Element.Visibility = (item.IsChecked ?? false) ? Visibility.Visible : Visibility.Collapsed;
                if (item.Tag is CompositeModelInstance)
                    (item.Tag as CompositeModelInstance).Element.Visibility = (item.IsChecked ?? false) ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                foreach (TreeItemModel i in item.Items)
                {
                    i.IsChecked = item.IsChecked;
                    if (i.Tag is ModelInstance)
                        (i.Tag as ModelInstance).Element.Visibility = (i.IsChecked ?? false) ? Visibility.Visible : Visibility.Collapsed;
                    if (i.Tag is CompositeModelInstance)
                        (i.Tag as CompositeModelInstance).Element.Visibility = (i.IsChecked ?? false) ? Visibility.Visible : Visibility.Collapsed;
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

            var compManagers = sceneryPalette
                .Concat(bipedPalette)
                .Concat(vehiclePalette)
                .Concat(equipmentPalette)
                .Concat(weaponPalette)
                .Concat(cratePalette);

            foreach (var man in compManagers)
            {
                man?.Model.Dispose();
                man?.Dispose();
            }

            foreach (var man in bspPalette)
            {
                man?.Model.Dispose();
                man?.Dispose();
            }

            sceneManager?.Dispose();
            renderer.Dispose();
            GC.Collect();
        }
        #endregion
    }
}
