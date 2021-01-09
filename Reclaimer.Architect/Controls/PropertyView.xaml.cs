﻿using Adjutant.Geometry;
using Adjutant.Spatial;
using Reclaimer.Models;
using Reclaimer.Models.Ai;
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
    public partial class PropertyView : IScenarioPropertyView, IMetaViewerHost
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
        private readonly List<Tuple<XmlNode, int>> altNodes;

        private ScenarioModel scenario;
        private MetaContext context; //dont dispose this because it will dispose our MetadataStream
        private XmlNode rootNode;
        private long baseAddress;

        public TabModel TabModel { get; }
        public object CurrentItem { get; private set; }
        public ObservableCollection<MetaValueBase> Metadata { get; }

        public PropertyView()
        {
            InitializeComponent();
            valuesById = new Dictionary<string, MetaValueBase>();
            altNodes = new List<Tuple<XmlNode, int>>();
            Metadata = new ObservableCollection<MetaValueBase>();
            TabModel = new TabModel(this, Studio.Controls.TabItemType.Tool) { Header = "Properties", ToolTip = "Property View" };
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
                single.SetValue(value);
                return;
            }

            var multi = meta as MultiValue;
            if (multi != null)
            {
                multi.SetValue((IXMVector)value);
                return;
            }
        }

        public void ShowProperties(NodeType nodeType, int itemIndex)
        {
            context = new MetaContext(scenario.Xml, scenario.ScenarioTag.CacheFile, scenario.ScenarioTag, scenario.MetadataStream);
            Visibility = Visibility.Hidden;
            rootNode = null;
            altNodes.Clear();

            var paletteKey = PaletteType.FromNodeType(nodeType);
            if (paletteKey != null && itemIndex >= 0)
            {
                var palette = scenario.Palettes[paletteKey];
                rootNode = palette.PlacementsNode;
                baseAddress = palette.PlacementBlockRef.TagBlock.Pointer.Address
                    + itemIndex * palette.PlacementBlockRef.BlockSize;

                altNodes.Add(Tuple.Create(palette.PaletteNode, 0));

                CurrentItem = palette.Placements[itemIndex];
                LoadData();
            }
            else
            {
                if (nodeType == NodeType.Mission)
                {
                    rootNode = scenario.Sections["mission"].Node;
                    baseAddress = scenario.RootAddress;
                    CurrentItem = null;
                    LoadData();
                }
                else if (nodeType == NodeType.StartPositions && itemIndex >= 0)
                {
                    var section = scenario.Sections["startpositions"];

                    rootNode = section.Node;
                    baseAddress = section.TagBlock.Pointer.Address
                        + itemIndex * section.BlockSize;

                    CurrentItem = scenario.StartingPositions[itemIndex];
                    LoadData();
                }
                else if (nodeType == NodeType.StartProfiles && itemIndex >= 0)
                {
                    var section = scenario.Sections["startprofiles"];

                    rootNode = section.Node;
                    baseAddress = section.TagBlock.Pointer.Address
                        + itemIndex * section.BlockSize;

                    CurrentItem = scenario.Items[itemIndex];
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
                else if ((nodeType == NodeType.AiZoneItem && scenario.SelectedNode.Tag is AiZone) || (nodeType == NodeType.AiZones && itemIndex >= 0))
                {
                    var zone = nodeType == NodeType.AiZones
                        ? scenario.SquadHierarchy.Zones[itemIndex]
                        : scenario.SelectedNode.Tag as AiZone;

                    rootNode = scenario.SquadHierarchy.AiNodes["zones"];
                    baseAddress = zone.BlockReference.TagBlock.Pointer.Address
                        + zone.BlockIndex * zone.BlockReference.BlockSize;

                    CurrentItem = zone;
                    LoadData();
                }
                else if ((nodeType == NodeType.AiEncounterItem && scenario.SelectedNode.Tag is AiEncounter) || (nodeType == NodeType.AiEncounters && itemIndex >= 0))
                {
                    var enc = nodeType == NodeType.AiEncounters
                        ? (scenario.SelectedNode.Parent.Tag as AiZone).Encounters[itemIndex]
                        : scenario.SelectedNode.Tag as AiEncounter;

                    rootNode = scenario.SquadHierarchy.AiNodes["encounters"];
                    baseAddress = enc.BlockReference.TagBlock.Pointer.Address
                        + enc.BlockIndex * enc.BlockReference.BlockSize;

                    altNodes.Add(Tuple.Create(scenario.SquadHierarchy.AiNodes["squadgroups"], 0));
                    altNodes.Add(Tuple.Create(scenario.SquadHierarchy.AiNodes["zones"], 0));

                    CurrentItem = enc;
                    LoadData();
                }
                else if ((nodeType == NodeType.AiSquadItem && scenario.SelectedNode.Tag is AiSquad) || (nodeType == NodeType.AiSquads && itemIndex >= 0))
                {
                    var squad = nodeType == NodeType.AiSquads
                        ? (scenario.SelectedNode.Parent.Tag as AiEncounter).Squads[itemIndex]
                        : scenario.SelectedNode.Tag as AiSquad;

                    rootNode = scenario.SquadHierarchy.AiNodes["squads"];
                    baseAddress = squad.BlockReference.TagBlock.Pointer.Address
                        + squad.BlockIndex * squad.BlockReference.BlockSize;

                    foreach (var palette in scenario.Palettes.Values)
                        altNodes.Add(Tuple.Create(palette.PaletteNode, 0));

                    CurrentItem = squad;
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

            if (altNodes.Count == 0)
                return;

            foreach (var t in altNodes)
                MetaValueBase.GetMetaValue(t.Item1, context, t.Item2);

            context.UpdateBlockIndices();
        }

        private void Meta_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var id = valuesById.FirstOrDefault(p => p.Value == sender).Key;
            if (id == null)
                return;

            (CurrentItem as IMetaUpdateReceiver)?.UpdateFromMetaValue(sender as MetaValueBase, id);
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
