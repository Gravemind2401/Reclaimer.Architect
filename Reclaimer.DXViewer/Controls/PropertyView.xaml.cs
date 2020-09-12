using Adjutant.Geometry;
using Adjutant.Spatial;
using Reclaimer.Models;
using Reclaimer.Plugins.MetaViewer;
using Reclaimer.Plugins.MetaViewer.Halo3;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace Reclaimer.Controls
{
    /// <summary>
    /// Interaction logic for PropertyView.xaml
    /// </summary>
    public partial class PropertyView : IScenarioPropertyView
    {
        #region Dependency Properties
        public static readonly DependencyProperty ShowInvisiblesProperty =
            DependencyProperty.Register(nameof(ShowInvisibles), typeof(bool), typeof(PropertyView), new PropertyMetadata(false, ShowInvisiblesChanged));

        public bool ShowInvisibles
        {
            get { return (bool)GetValue(ShowInvisiblesProperty); }
            set { SetValue(ShowInvisiblesProperty, value); }
        }

        public static void ShowInvisiblesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //MetaViewerPlugin.Settings.ShowInvisibles = e.NewValue as bool? ?? false;
        }
        #endregion

        private readonly Dictionary<string, MetaValueBase> valuesById;

        private ScenarioModel scenario;
        private MetaContext context;
        private XmlNode rootNode;
        private long baseAddress;

        public TabModel TabModel { get; }
        public object CurrentItem { get; private set; }
        public ObservableCollection<MetaValueBase> Metadata { get; }

        public PropertyView()
        {
            InitializeComponent();
            valuesById = new Dictionary<string, MetaValueBase>();
            Metadata = new ObservableCollection<MetaValueBase>();
            TabModel = new TabModel(this, Studio.Controls.TabItemType.Tool) { Header = "Properties" };
            DataContext = this;
        }

        public void ClearScenario()
        {
            ClearProperties();
            scenario = null;
        }

        public void SetScenario(ScenarioModel scenario)
        {
            this.scenario = scenario;
            context = new MetaContext(scenario.ScenarioTag.CacheFile, scenario.ScenarioTag, scenario.Transaction);
        }

        public void ClearProperties()
        {
            Metadata.Clear();
            Visibility = Visibility.Hidden;
        }

        public void SetValue(string id, object value)
        {
            if (!valuesById.ContainsKey(id))
                return;

            var meta = valuesById[id];

            var single = meta as SimpleValue;
            if (single != null)
            {
                single.Value = value;
                return;
            }

            var multi = meta as MultiValue;
            if (multi != null)
            {
                var vector = (IXMVector)value;
                multi.Value1 = vector.X;
                multi.Value2 = vector.Y;
                multi.Value3 = vector.Z;
                multi.Value4 = vector.W;
            }
        }

        public void ShowProperties(NodeType nodeType, int itemIndex)
        {
            Visibility = Visibility.Hidden;

            var paletteKey = PaletteType.FromNodeType(nodeType);
            if (paletteKey != null && itemIndex >= 0)
            {
                var palette = scenario.Palettes[paletteKey];
                rootNode = palette.PlacementsNode;
                baseAddress = palette.PlacementBlockRef.TagBlock.Pointer.Address
                    + itemIndex * palette.PlacementBlockRef.BlockSize;

                CurrentItem = palette.Placements[itemIndex];
                LoadData();
            }
            else
            {
                if (nodeType == NodeType.Mission)
                {
                    rootNode = scenario.Sections["mission"].Node;
                    baseAddress = scenario.ScenarioTag.MetaPointer.Address;
                    CurrentItem = null;
                    LoadData();
                }
                else if (nodeType == NodeType.TriggerVolumes && itemIndex >= 0)
                {
                    var section = scenario.Sections["triggervolumes"];

                    rootNode = section.Node;
                    baseAddress = section.TagBlock.Pointer.Address
                        + itemIndex * section.BlockSize;

                    CurrentItem = scenario.TriggerVolumes[itemIndex];
                    LoadData();
                }
                else
                {
                    CurrentItem = null;
                    return;
                }
            }

            Visibility = Visibility.Visible;
        }

        private void LoadData()
        {
            foreach (var meta in valuesById.Values)
                meta.PropertyChanged -= Meta_PropertyChanged;
            Metadata.Clear();
            valuesById.Clear();
            foreach (XmlNode n in rootNode.ChildNodes)
            {
                try
                {
                    var meta = MetaValueBase.GetMetaValue(n, context, baseAddress);
                    Metadata.Add(meta);
                    if (n.Attributes["id"] != null)
                    {
                        valuesById.Add(n.Attributes["id"].Value, meta);
                        meta.PropertyChanged += Meta_PropertyChanged;
                    }
                }
                catch { }
            }
        }

        private void Meta_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var id = valuesById.FirstOrDefault(p => p.Value == sender).Key;
            if (id == null)
                return;

            var placement = CurrentItem as ObjectPlacement;
            if (placement != null)
            {
                switch (id)
                {
                    case "position":
                    case "rotation":
                        var multi = sender as MultiValue;
                        var vector = new RealVector3D(multi.Value1, multi.Value2, multi.Value3);
                        if (id == "position")
                            placement.Position = vector;
                        else placement.Rotation = vector;
                        break;
                    case "scale":
                        var simple = sender as SimpleValue;
                        placement.Scale = float.Parse(simple.Value.ToString());
                        break;
                }

                return;
            }

            var volume = CurrentItem as TriggerVolume;
            if (volume != null)
            {
                switch (id)
                {
                    case "position":
                    case "size":
                        var multi = sender as MultiValue;
                        var vector = new RealVector3D(multi.Value1, multi.Value2, multi.Value3);
                        if (id == "position")
                            volume.Position = vector;
                        else volume.Size = vector;
                        break;
                }

                return;
            }
        }

        private void RecursiveToggle(IEnumerable<MetaValueBase> collection, bool value)
        {
            foreach (var s in collection.OfType<IExpandable>())
            {
                s.IsExpanded = value;
                RecursiveToggle(s.Children, value);
            }
        }

        private void btnReload_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void btnCollapseAll_Click(object sender, RoutedEventArgs e)
        {
            RecursiveToggle(Metadata, false);
        }

        private void btnExpandAll_Click(object sender, RoutedEventArgs e)
        {
            RecursiveToggle(Metadata, true);
        }
    }
}
