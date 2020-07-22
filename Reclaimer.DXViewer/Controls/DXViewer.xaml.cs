﻿using Adjutant.Geometry;
using Adjutant.Utilities;
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

namespace Reclaimer.Controls
{
    /// <summary>
    /// Interaction logic for DXViewer.xaml
    /// </summary>
    public partial class DXViewer : UserControl
    {
        private static readonly string[] AllLods = new[] { "Highest", "High", "Medium", "Low", "Lowest" };
        private static readonly DiffuseMaterial ErrorMaterial;

        #region Dependency Properties
        private static readonly DependencyPropertyKey AvailableLodsPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(AvailableLods), typeof(IEnumerable<string>), typeof(ModelViewer), new PropertyMetadata());

        public static readonly DependencyProperty AvailableLodsProperty = AvailableLodsPropertyKey.DependencyProperty;

        public static readonly DependencyProperty SelectedLodProperty =
            DependencyProperty.Register(nameof(SelectedLod), typeof(int), typeof(ModelViewer), new PropertyMetadata(0, SelectedLodChanged));

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
            control.SetLod((int)e.NewValue);
        }
        #endregion

        private readonly Model3DGroup modelGroup = new Model3DGroup();
        private readonly ModelVisual3D visual = new ModelVisual3D();

        private IRenderGeometry geometry;
        private IGeometryModel model;

        public TabModel TabModel { get; }
        public ObservableCollection<TreeItemModel> TreeViewItems { get; }

        public Action<string> LogOutput { get; set; }
        public Action<string, Exception> LogError { get; set; }
        public Action<string> SetStatus { get; set; }
        public Action ClearStatus { get; set; }

        static DXViewer()
        {
            (ErrorMaterial = new DiffuseMaterial(Brushes.Gold)).Freeze();
        }

        public DXViewer()
        {
            InitializeComponent();
            TabModel = new TabModel(this, TabItemType.Document);
            TreeViewItems = new ObservableCollection<TreeItemModel>();
            DataContext = this;

            visual.Content = modelGroup;
            renderer.AddChild(visual);
        }

        public void LoadGeometry(IRenderGeometry geometry, string fileName)
        {
            TabModel.ToolTip = fileName;
            TabModel.Header = Utils.GetFileName(fileName);
            this.geometry = geometry;

            AvailableLods = AllLods.Take(geometry.LodCount);
            SetLod(0);
        }

        private void SetLod(int index)
        {
            model = geometry.ReadGeometry(index);
            var meshes = GetMeshes(model).ToList();

            TreeViewItems.Clear();
            modelGroup.Children.Clear();
            foreach (var region in model.Regions)
            {
                var regNode = new TreeItemModel { Header = region.Name, IsChecked = true };

                foreach (var perm in region.Permutations)
                {
                    var mesh = meshes[perm.MeshIndex];
                    if (mesh == null)
                        continue;

                    var permNode = new TreeItemModel { Header = perm.Name, IsChecked = true };
                    regNode.Items.Add(permNode);

                    var tGroup = new Transform3DGroup();

                    if (perm.TransformScale != 1)
                    {
                        var tform = new ScaleTransform3D(perm.TransformScale, perm.TransformScale, perm.TransformScale);

                        tform.Freeze();
                        tGroup.Children.Add(tform);
                    }

                    if (!perm.Transform.IsIdentity)
                    {
                        var tform = new MatrixTransform3D(new Matrix3D
                        {
                            M11 = perm.Transform.M11,
                            M12 = perm.Transform.M12,
                            M13 = perm.Transform.M13,

                            M21 = perm.Transform.M21,
                            M22 = perm.Transform.M22,
                            M23 = perm.Transform.M23,

                            M31 = perm.Transform.M31,
                            M32 = perm.Transform.M32,
                            M33 = perm.Transform.M33,

                            OffsetX = perm.Transform.M41,
                            OffsetY = perm.Transform.M42,
                            OffsetZ = perm.Transform.M43
                        });

                        tform.Freeze();
                        tGroup.Children.Add(tform);
                    }

                    Model3DGroup permGroup;
                    if (tGroup.Children.Count == 0 && perm.MeshCount == 1)
                        permGroup = meshes[perm.MeshIndex];
                    else
                    {
                        permGroup = new Model3DGroup();
                        for (int i = 0; i < perm.MeshCount; i++)
                        {
                            if (tGroup.Children.Count > 0)
                            {
                                (permGroup.Transform = tGroup).Freeze();

                                permGroup.Children.Add(meshes[perm.MeshIndex + i]);
                                permGroup.Freeze();
                            }
                            else permGroup.Children.Add(meshes[perm.MeshIndex + i]);
                        }
                    }

                    permNode.Tag = permGroup;
                    modelGroup.Children.Add(permGroup);
                }

                if (regNode.HasItems)
                    TreeViewItems.Add(regNode);
            }

            renderer.ScaleToContent(new[] { modelGroup });
        }

        private IEnumerable<Material> GetMaterials(IGeometryModel model)
        {
            var indexes = model.Meshes.SelectMany(m => m.Submeshes)
                .Select(s => s.MaterialIndex).Distinct().ToArray();

            for (short i = 0; i < model.Materials.Count; i++)
            {
                if (!indexes.Contains(i))
                {
                    yield return null;
                    continue;
                }

                var mat = model.Materials[i];
                DiffuseMaterial material;

                try
                {
                    var diffuse = mat.Submaterials.First(m => m.Usage == MaterialUsage.Diffuse);
                    var dds = diffuse.Bitmap.ToDds(0);

                    var brush = new ImageBrush(dds.ToBitmapSource(DecompressOptions.Bgr24))
                    {
                        ViewportUnits = BrushMappingMode.Absolute,
                        TileMode = TileMode.Tile,
                        Viewport = new Rect(0, 0, 1f / Math.Abs(diffuse.Tiling.X), 1f / Math.Abs(diffuse.Tiling.Y))
                    };

                    brush.Freeze();
                    material = new DiffuseMaterial(brush);
                    material.Freeze();
                }
                catch
                {
                    material = ErrorMaterial;
                }

                yield return material;
            }
        }

        private IEnumerable<Model3DGroup> GetMeshes(IGeometryModel model)
        {
            var indexes = model.Regions.SelectMany(r => r.Permutations)
                .SelectMany(p => Enumerable.Range(p.MeshIndex, p.MeshCount))
                .Distinct().ToList();

            var materials = GetMaterials(model).ToList();

            for (int i = 0; i < model.Meshes.Count; i++)
            {
                var mesh = model.Meshes[i];

                if (mesh.Submeshes.Count == 0 || !indexes.Contains(i))
                {
                    yield return null;
                    continue;
                }

                var mGroup = new Model3DGroup();
                var tGroup = new Transform3DGroup();

                var texMatrix = Matrix.Identity;
                if (mesh.BoundsIndex >= 0)
                {
                    var bounds = model.Bounds[mesh.BoundsIndex.Value];
                    texMatrix = new Matrix
                    {
                        M11 = bounds.UBounds.Length,
                        M22 = bounds.VBounds.Length,
                        OffsetX = bounds.UBounds.Min,
                        OffsetY = bounds.VBounds.Min
                    };

                    var transform = new Matrix3D
                    {
                        M11 = bounds.XBounds.Length,
                        M22 = bounds.YBounds.Length,
                        M33 = bounds.ZBounds.Length,
                        OffsetX = bounds.XBounds.Min,
                        OffsetY = bounds.YBounds.Min,
                        OffsetZ = bounds.ZBounds.Min
                    };

                    var tform = new MatrixTransform3D(transform);
                    tform.Freeze();
                    tGroup.Children.Add(tform);
                }

                foreach (var sub in mesh.Submeshes)
                {
                    try
                    {
                        var geom = new MeshGeometry3D();

                        var indices = mesh.Indicies.Skip(sub.IndexStart).Take(sub.IndexLength).ToList();
                        if (mesh.IndexFormat == IndexFormat.TriangleStrip) indices = indices.Unstrip().ToList();

                        var vertStart = indices.Min();
                        var vertLength = indices.Max() - vertStart + 1;

                        var verts = mesh.Vertices.Skip(vertStart).Take(vertLength);
                        var positions = verts.Select(v => new Point3D(v.Position[0].X, v.Position[0].Y, v.Position[0].Z));

                        var texcoords = verts.Select(v => new Point(v.TexCoords[0].X, v.TexCoords[0].Y)).ToArray();
                        if (!texMatrix.IsIdentity) texMatrix.Transform(texcoords);

                        (geom.Positions = new Point3DCollection(positions)).Freeze();
                        (geom.TextureCoordinates = new PointCollection(texcoords)).Freeze();
                        (geom.TriangleIndices = new Int32Collection(indices.Select(j => j - vertStart))).Freeze();

                        if (mesh.Vertices[0].Normal.Count > 0)
                        {
                            var normals = verts.Select(v => new Vector3D(v.Normal[0].X, v.Normal[0].Y, v.Normal[0].Z));
                            (geom.Normals = new Vector3DCollection(normals)).Freeze();
                        }

                        var mat = sub.MaterialIndex >= 0 ? materials[sub.MaterialIndex] : ErrorMaterial;
                        var subGroup = new GeometryModel3D(geom, mat) { BackMaterial = mat };
                        subGroup.Freeze();
                        mGroup.Children.Add(subGroup);
                    }
                    catch { }
                }

                (mGroup.Transform = tGroup).Freeze();
                mGroup.Freeze();

                yield return mGroup;
            }
        }

        #region Treeview Events
        private void TreeViewItem_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as TreeItemModel;
            if (item != tv.SelectedItem)
                return; //because this event bubbles to the parent node

            var mesh = item.Tag as Model3DGroup;
            if (mesh != null)
                renderer.LocateObject(mesh);
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

                var group = item.Tag as Model3DGroup;
                if (item.IsChecked == true)
                    modelGroup.Children.Add(group);
                else
                    modelGroup.Children.Remove(group);
            }
            else
            {
                foreach (TreeItemModel i in item.Items)
                {
                    var group = i.Tag as Model3DGroup;
                    i.IsChecked = item.IsChecked;
                    if (i.IsChecked == true)
                        modelGroup.Children.Add(group);
                    else
                        modelGroup.Children.Remove(group);
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

        #region IDisposable
        public void Dispose()
        {
            TreeViewItems.Clear();
            renderer.Dispose();
            GC.Collect();
        }
        #endregion
    }
}
