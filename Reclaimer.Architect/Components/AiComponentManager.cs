using Reclaimer.Controls.Markers;
using Reclaimer.Geometry;
using Reclaimer.Models;
using Reclaimer.Models.Ai;
using Reclaimer.Resources;
using Reclaimer.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Xml;

using Media = System.Windows.Media;
using Media3D = System.Windows.Media.Media3D;
using Helix = HelixToolkit.Wpf.SharpDX;

namespace Reclaimer.Components
{
    public class AiComponentManager : ComponentManager
    {
        private readonly Dictionary<AiZone, Helix.GroupElement3D> AreaGroups;
        private readonly Dictionary<AiZone, ObservableCollection<PositionMarker3D>> Areas;
        private readonly Dictionary<AiZone, Helix.GroupElement3D> FiringPositionGroups;
        private readonly Dictionary<AiZone, ObservableCollection<PositionMarker3D>> FiringPositions;
        private readonly Dictionary<AiEncounter, Helix.GroupElement3D> StartLocationGroups;
        private readonly Dictionary<AiEncounter, ObservableCollection<SpawnPointMarker3D>> StartLocations;

        private readonly Dictionary<AiSquad, Helix.GroupElement3D> GroupStartLocationGroups;
        private readonly Dictionary<AiSquad, ObservableCollection<SpawnPointMarker3D>> GroupStartLocations;
        private readonly Dictionary<AiSquad, Helix.GroupElement3D> SoloStartLocationGroups;
        private readonly Dictionary<AiSquad, ObservableCollection<SpawnPointMarker3D>> SoloStartLocations;

        private IEnumerable<NodeType> HandledNodeTypes
        {
            get
            {
                yield return NodeType.AiSquadGroups;
                yield return NodeType.AiZones;
                yield return NodeType.AiZoneItem;
                yield return NodeType.AiFiringPositions;
                yield return NodeType.AiZoneAreas;
                yield return NodeType.AiSquads;
                yield return NodeType.AiSquadItem;
                yield return NodeType.AiEncounters;
                yield return NodeType.AiEncounterItem;
                yield return NodeType.AiStartingLocations;
                yield return NodeType.AiGroupStartingLocations;
                yield return NodeType.AiSoloStartingLocations;
            }
        }

        public AiComponentManager(ScenarioModel scenario)
            : base(scenario)
        {
            AreaGroups = new Dictionary<AiZone, Helix.GroupElement3D>();
            Areas = new Dictionary<AiZone, ObservableCollection<PositionMarker3D>>();
            FiringPositionGroups = new Dictionary<AiZone, Helix.GroupElement3D>();
            FiringPositions = new Dictionary<AiZone, ObservableCollection<PositionMarker3D>>();
            StartLocationGroups = new Dictionary<AiEncounter, Helix.GroupElement3D>();
            StartLocations = new Dictionary<AiEncounter, ObservableCollection<SpawnPointMarker3D>>();

            GroupStartLocationGroups = new Dictionary<AiSquad, Helix.GroupElement3D>();
            GroupStartLocations = new Dictionary<AiSquad, ObservableCollection<SpawnPointMarker3D>>();
            SoloStartLocationGroups = new Dictionary<AiSquad, Helix.GroupElement3D>();
            SoloStartLocations = new Dictionary<AiSquad, ObservableCollection<SpawnPointMarker3D>>();
        }

        public override bool HandlesNodeType(NodeType nodeType) => HandledNodeTypes.Any(t => t == nodeType);

        public override void InitializeElements(ModelFactory factory)
        {
            foreach (var zone in scenario.SquadHierarchy.Zones)
            {
                #region Firing Positions
                var fposGroup = new Helix.GroupModel3D();
                var fposMarkers = new ObservableCollection<PositionMarker3D>();

                foreach (var area in zone.FiringPositions)
                {
                    var fposMarker = new PositionMarker3D();
                    BindFiringPosition(area, fposMarker);
                    fposMarkers.Add(fposMarker);
                    fposGroup.Children.Add(fposMarker);
                }

                FiringPositionGroups.Add(zone, fposGroup);
                FiringPositions.Add(zone, fposMarkers);
                #endregion

                #region Areas
                var areaGroup = new Helix.GroupModel3D();
                var areaMarkers = new ObservableCollection<PositionMarker3D>();

                foreach (var area in zone.Areas)
                {
                    var areaMarker = new PositionMarker3D();
                    BindArea(area, areaMarker);
                    areaMarkers.Add(areaMarker);
                    areaGroup.Children.Add(areaMarker);
                }

                AreaGroups.Add(zone, areaGroup);
                Areas.Add(zone, areaMarkers);
                #endregion

                foreach (var squad in zone.Squads)
                {
                    Helix.GroupModel3D locGroup;
                    ObservableCollection<SpawnPointMarker3D> locMarkers;

                    #region Encounter Starting Locations
                    foreach (var enc in squad.Encounters)
                    {
                        locGroup = new Helix.GroupModel3D();
                        locMarkers = new ObservableCollection<SpawnPointMarker3D>();

                        foreach (var loc in enc.StartingLocations)
                        {
                            var locMarker = new SpawnPointMarker3D();
                            BindStartLocation(loc, locMarker);
                            locMarkers.Add(locMarker);
                            locGroup.Children.Add(locMarker);
                        }

                        StartLocationGroups.Add(enc, locGroup);
                        StartLocations.Add(enc, locMarkers);
                    }
                    #endregion

                    #region Formation Locations
                    locGroup = new Helix.GroupModel3D();
                    locMarkers = new ObservableCollection<SpawnPointMarker3D>();

                    foreach (var loc in squad.GroupStartLocations)
                    {
                        var locMarker = new SpawnPointMarker3D();
                        BindStartLocation(loc, locMarker);
                        locMarkers.Add(locMarker);
                        locGroup.Children.Add(locMarker);
                    }

                    GroupStartLocationGroups.Add(squad, locGroup);
                    GroupStartLocations.Add(squad, locMarkers);
                    #endregion

                    #region Spawn Locations
                    locGroup = new Helix.GroupModel3D();
                    locMarkers = new ObservableCollection<SpawnPointMarker3D>();

                    foreach (var loc in squad.SoloStartLocations)
                    {
                        var locMarker = new SpawnPointMarker3D();
                        BindStartLocation(loc, locMarker);
                        locMarkers.Add(locMarker);
                        locGroup.Children.Add(locMarker);
                    }

                    SoloStartLocationGroups.Add(squad, locGroup);
                    SoloStartLocations.Add(squad, locMarkers);
                    #endregion
                }
            }
        }

        public override IEnumerable<Helix.Element3D> GetSceneElements()
        {
            return FiringPositionGroups.Values
                .Concat(AreaGroups.Values)
                .Concat(StartLocationGroups.Values)
                .Concat(GroupStartLocationGroups.Values)
                .Concat(SoloStartLocationGroups.Values);
        }

        public override void OnSelectedTreeNodeChanged(SceneNodeModel newNode)
        {
            var nodeType = newNode?.NodeType ?? NodeType.None;
            var nodeTag = newNode?.Tag;

            foreach (var pair in FiringPositionGroups)
                pair.Value.IsRendering = nodeType == NodeType.AiFiringPositions && pair.Key == nodeTag;

            foreach (var pair in AreaGroups)
                pair.Value.IsRendering = nodeType == NodeType.AiZoneAreas && pair.Key == nodeTag;

            foreach (var pair in StartLocationGroups)
                pair.Value.IsRendering = nodeType == NodeType.AiStartingLocations && pair.Key == nodeTag;

            foreach (var pair in GroupStartLocationGroups)
                pair.Value.IsRendering = nodeType == NodeType.AiGroupStartingLocations && pair.Key == nodeTag;

            foreach (var pair in SoloStartLocationGroups)
                pair.Value.IsRendering = nodeType == NodeType.AiSoloStartingLocations && pair.Key == nodeTag;
        }

        public override Helix.Element3D GetElement(SceneNodeModel treeNode, int itemIndex)
        {
            var nodeTag = treeNode.Tag;

            if (treeNode.NodeType == NodeType.AiFiringPositions)
                return FiringPositions[nodeTag as AiZone][itemIndex];
            else if (treeNode.NodeType == NodeType.AiZoneAreas)
                return Areas[nodeTag as AiZone][itemIndex];
            else if (treeNode.NodeType == NodeType.AiStartingLocations)
                return StartLocations[nodeTag as AiEncounter][itemIndex];
            else if (treeNode.NodeType == NodeType.AiGroupStartingLocations)
                return GroupStartLocations[nodeTag as AiSquad][itemIndex];
            else if (treeNode.NodeType == NodeType.AiSoloStartingLocations)
                return SoloStartLocations[nodeTag as AiSquad][itemIndex];
            else
                return null;
        }

        public override SharpDX.BoundingBox GetObjectBounds(SceneNodeModel treeNode, int itemIndex)
        {
            return GetElement(treeNode, itemIndex).GetTotalBounds();
        }

        public override int GetElementIndex(SceneNodeModel treeNode, Helix.Element3D element)
        {
            var nodeTag = treeNode.Tag;

            if (treeNode.NodeType == NodeType.AiFiringPositions)
                return (nodeTag as AiZone).FiringPositions.IndexOf(element.DataContext as AiFiringPosition);
            else if (treeNode.NodeType == NodeType.AiZoneAreas)
                return (nodeTag as AiZone).Areas.IndexOf(element.DataContext as AiArea);
            else if (treeNode.NodeType == NodeType.AiStartingLocations)
                return (nodeTag as AiEncounter).StartingLocations.IndexOf(element.DataContext as AiStartingLocation);
            else if (treeNode.NodeType == NodeType.AiGroupStartingLocations)
                return (nodeTag as AiSquad).GroupStartLocations.IndexOf(element.DataContext as AiStartingLocation);
            else if (treeNode.NodeType == NodeType.AiSoloStartingLocations)
                return (nodeTag as AiSquad).SoloStartLocations.IndexOf(element.DataContext as AiStartingLocation);
            else throw new ArgumentException();
        }

        public override IEnumerable<ScenarioListItem> GetListItems(SceneNodeModel treeNode)
        {
            var zoneTag = treeNode.Parent.Tag as AiZone;
            var encTag = treeNode.Parent.Tag as AiEncounter;
            var squadTag = treeNode.Parent.Tag as AiSquad;

            if (treeNode.NodeType == NodeType.AiSquadGroups)
                return scenario.SquadHierarchy.SquadGroups.Select(group => new ScenarioListItem(group.Name, group));
            else if (treeNode.NodeType == NodeType.AiZones)
                return scenario.SquadHierarchy.Zones.Select(zone => new ScenarioListItem(zone.Name, zone));
            else if (treeNode.NodeType == NodeType.AiZoneAreas)
                return zoneTag.Areas.Select(area => new ScenarioListItem(area.GetDisplayName(), area));
            else if (treeNode.NodeType == NodeType.AiFiringPositions)
                return zoneTag.FiringPositions.Select(fpos => new ScenarioListItem(fpos.GetDisplayName(), fpos));
            else if (treeNode.NodeType == NodeType.AiSquads)
                return zoneTag.Squads.Select(squad => new ScenarioListItem(squad.Name, squad));
            else if (treeNode.NodeType == NodeType.AiEncounters)
                return squadTag.Encounters.Select(enc => new ScenarioListItem(enc.Name, enc));
            else if (treeNode.NodeType == NodeType.AiStartingLocations
                || treeNode.NodeType == NodeType.AiGroupStartingLocations
                || treeNode.NodeType == NodeType.AiSoloStartingLocations)
            {
                var locations = treeNode.NodeType == NodeType.AiStartingLocations
                    ? encTag.StartingLocations
                    : treeNode.NodeType == NodeType.AiGroupStartingLocations
                        ? squadTag.GroupStartLocations
                        : squadTag.SoloStartLocations;

                return locations.Select(loc => new ScenarioListItem(loc.GetDisplayName(), loc));
            }
            else return Enumerable.Empty<ScenarioListItem>();
        }

        internal override BlockPropertiesLocator GetPropertiesLocator(SceneNodeModel treeNode, int itemIndex)
        {
            XmlNode rootNode;
            long baseAddress;
            IMetaUpdateReceiver target;
            var altNodes = new List<Tuple<XmlNode, long>>();

            if (treeNode.NodeType == NodeType.AiSquadGroups && itemIndex >= 0)
            {
                var group = scenario.SquadHierarchy.SquadGroups[itemIndex];

                rootNode = scenario.SquadHierarchy.AiNodes[AiSection.SquadGroups];
                baseAddress = group.BlockStartAddress;

                altNodes.Add(Tuple.Create(scenario.SquadHierarchy.AiNodes[AiSection.SquadGroups], scenario.RootAddress));

                target = group;
            }
            else if ((treeNode.NodeType == NodeType.AiZoneItem && treeNode.Tag is AiZone) || (treeNode.NodeType == NodeType.AiZones && itemIndex >= 0))
            {
                var zone = treeNode.NodeType == NodeType.AiZones
                    ? scenario.SquadHierarchy.Zones[itemIndex]
                    : treeNode.Tag as AiZone;

                rootNode = scenario.SquadHierarchy.AiNodes[AiSection.Zones];
                baseAddress = zone.BlockStartAddress;

                target = zone;
            }
            else if (treeNode.NodeType == NodeType.AiFiringPositions && itemIndex >= 0)
            {
                var fpos = (treeNode.Parent.Tag as AiZone).FiringPositions[itemIndex];

                rootNode = scenario.SquadHierarchy.AiNodes[AiSection.FiringPositions];
                baseAddress = fpos.BlockReference.TagBlock.Pointer.Address
                    + fpos.BlockIndex * fpos.BlockReference.BlockSize;

                altNodes.Add(Tuple.Create(scenario.SquadHierarchy.AiNodes[AiSection.Areas], fpos.Zone.BlockStartAddress));

                target = fpos;
            }
            else if (treeNode.NodeType == NodeType.AiZoneAreas && itemIndex >= 0)
            {
                var area = (treeNode.Parent.Tag as AiZone).Areas[itemIndex];

                rootNode = scenario.SquadHierarchy.AiNodes[AiSection.Areas];
                baseAddress = area.BlockReference.TagBlock.Pointer.Address
                    + area.BlockIndex * area.BlockReference.BlockSize;

                target = area;
            }
            else if ((treeNode.NodeType == NodeType.AiSquadItem && treeNode.Tag is AiSquad) || (treeNode.NodeType == NodeType.AiSquads && itemIndex >= 0))
            {
                var squad = treeNode.NodeType == NodeType.AiSquads
                    ? (treeNode.Parent.Tag as AiZone).Squads[itemIndex]
                    : treeNode.Tag as AiSquad;

                rootNode = scenario.SquadHierarchy.AiNodes[AiSection.Squads];
                baseAddress = squad.BlockStartAddress;

                altNodes.Add(Tuple.Create(scenario.SquadHierarchy.AiNodes[AiSection.SquadGroups], scenario.RootAddress));
                altNodes.Add(Tuple.Create(scenario.SquadHierarchy.AiNodes[AiSection.Zones], scenario.RootAddress));

                target = squad;
            }
            else if ((treeNode.NodeType == NodeType.AiEncounterItem && treeNode.Tag is AiEncounter) || (treeNode.NodeType == NodeType.AiEncounters && itemIndex >= 0))
            {
                var enc = treeNode.NodeType == NodeType.AiEncounters
                    ? (treeNode.Parent.Tag as AiSquad).Encounters[itemIndex]
                    : treeNode.Tag as AiEncounter;

                rootNode = scenario.SquadHierarchy.AiNodes[AiSection.Encounters];
                baseAddress = enc.BlockStartAddress;

                foreach (var palette in scenario.Palettes.Values)
                    altNodes.Add(Tuple.Create(palette.PaletteNode, scenario.RootAddress));

                target = enc;
            }
            else if ((treeNode.NodeType == NodeType.AiStartingLocations
                || treeNode.NodeType == NodeType.AiGroupStartingLocations
                || treeNode.NodeType == NodeType.AiSoloStartingLocations) && itemIndex >= 0)
            {
                var tag = treeNode.Parent.Tag;
                var loc = treeNode.NodeType == NodeType.AiStartingLocations
                ? (tag as AiEncounter).StartingLocations[itemIndex]
                : treeNode.NodeType == NodeType.AiGroupStartingLocations
                    ? (tag as AiSquad).GroupStartLocations[itemIndex]
                    : (tag as AiSquad).SoloStartLocations[itemIndex];

                rootNode = scenario.SquadHierarchy.AiNodes[loc.SectionKey];
                baseAddress = loc.BlockReference.TagBlock.Pointer.Address
                    + loc.BlockIndex * loc.BlockReference.BlockSize;

                foreach (var palette in scenario.Palettes.Values)
                    altNodes.Add(Tuple.Create(palette.PaletteNode, scenario.RootAddress));

                target = loc;
            }
            else return null;

            return new BlockPropertiesLocator
            {
                RootNode = rootNode,
                BaseAddress = baseAddress,
                AdditionalNodes = altNodes,
                TargetObject = target
            };
        }

        public override void DisposeSceneElements()
        {
            foreach (var element in GetSceneElements())
                element.Dispose();

            AreaGroups.Clear();
            Areas.Clear();
            FiringPositionGroups.Clear();
            FiringPositions.Clear();
            StartLocationGroups.Clear();
            StartLocations.Clear();

            GroupStartLocationGroups.Clear();
            GroupStartLocations.Clear();
            SoloStartLocationGroups.Clear();
            SoloStartLocations.Clear();
        }

        #region Binding Setup
        private void BindArea(AiArea area, Helix.Element3D model)
        {
            var binding = new Binding(nameof(AiArea.Position)) { Converter = TranslationTransformConverter.Instance, Mode = BindingMode.TwoWay };

            model.DataContext = area;
            BindingOperations.SetBinding(model, Helix.Element3D.TransformProperty, binding);
        }

        private void BindFiringPosition(AiFiringPosition fpos, Helix.Element3D model)
        {
            var binding = new Binding(nameof(AiFiringPosition.Position)) { Converter = TranslationTransformConverter.Instance, Mode = BindingMode.TwoWay };

            model.DataContext = fpos;
            BindingOperations.SetBinding(model, Helix.Element3D.TransformProperty, binding);
        }

        private void BindStartLocation(AiStartingLocation pos, Helix.Element3D model)
        {
            var binding = new MultiBinding { Converter = EulerTransformConverter.Instance, Mode = BindingMode.TwoWay };
            binding.Bindings.Add(new Binding(nameof(AiStartingLocation.Position)) { Mode = BindingMode.TwoWay });
            binding.Bindings.Add(new Binding(nameof(AiStartingLocation.Rotation)) { Mode = BindingMode.TwoWay });

            model.DataContext = pos;
            BindingOperations.SetBinding(model, Helix.Element3D.TransformProperty, binding);
        }
        #endregion
    }
}
