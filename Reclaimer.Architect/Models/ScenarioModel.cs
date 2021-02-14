using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Reclaimer.Utilities;
using System.Collections.ObjectModel;
using Prism.Mvvm;
using Adjutant.Blam.Common;
using Reclaimer.Plugins.MetaViewer;
using Adjutant.Spatial;
using System.Windows.Controls;
using System.IO.Endian;
using System.Runtime.CompilerServices;
using System.IO;
using Reclaimer.Resources;
using Adjutant.Blam.Common.Gen3;
using Reclaimer.Models.Ai;
using Reclaimer.Components;

namespace Reclaimer.Models
{
    public class ScenarioModel : BindableBase
    {
        internal bool IsBusy { get; private set; }
        internal Action<string, Exception> LogError { get; set; }

        public IIndexItem ScenarioTag { get; }
        public long RootAddress => 0; // ScenarioTag.MetaPointer.Address;

        public XmlDocument Xml { get; }
        public InMemoryMetadataStream MetadataStream { get; }

        internal List<ComponentManager> ComponentManagers { get; }

        public Dictionary<string, ScenarioSection> Sections { get; }
        public ObservableCollection<SceneNodeModel> Hierarchy { get; }
        public ObservableCollection<ScenarioListItem> Items { get; }

        public ObservableCollection<TagReference> Bsps { get; }
        public ObservableCollection<TagReference> Skies { get; }
        public List<string> ObjectNames { get; }

        public Dictionary<string, PaletteDefinition> Palettes { get; }
        public ObservableCollection<StartPosition> StartingPositions { get; }
        public ObservableCollection<TriggerVolume> TriggerVolumes { get; }

        public AiSquadHierarchy SquadHierarchy { get; }

        private IScenarioHierarchyView hierarchyView;
        public IScenarioHierarchyView HierarchyView
        {
            get { return hierarchyView; }
            set
            {
                var prev = hierarchyView;
                if (SetProperty(ref hierarchyView, value))
                {
                    prev?.ClearScenario();
                    hierarchyView?.SetScenario(this);
                }
            }
        }

        private IScenarioPropertyView propertyView;
        public IScenarioPropertyView PropertyView
        {
            get { return propertyView; }
            set
            {
                var prev = propertyView;
                if (SetProperty(ref propertyView, value))
                {
                    prev?.ClearScenario();
                    propertyView?.SetScenario(this);
                }
            }
        }

        private IScenarioRenderView renderView;
        public IScenarioRenderView RenderView
        {
            get { return renderView; }
            set
            {
                var prev = renderView;
                if (SetProperty(ref renderView, value))
                {
                    prev?.ClearScenario();
                    renderView?.SetScenario(this);
                }
            }
        }

        private SceneNodeModel selectedNode;
        public SceneNodeModel SelectedNode
        {
            get { return selectedNode; }
            set
            {
                if (SetProperty(ref selectedNode, value))
                {
                    RaisePropertyChanged(nameof(SelectedNodeType));
                    OnSelectedNodeChanged();
                }
            }
        }

        public NodeType SelectedNodeType => SelectedNode?.NodeType ?? NodeType.None;

        private int selectedItemIndex;
        public int SelectedItemIndex
        {
            get { return selectedItemIndex; }
            set
            {
                if (SetProperty(ref selectedItemIndex, value))
                    OnSelectedItemChanged();
            }
        }

        public ScenarioModel(IIndexItem item)
        {
            IsBusy = true;

            ScenarioTag = item;

            Xml = new XmlDocument();
            MetadataStream = new InMemoryMetadataStream(item, GetXmlData(ScenarioTag.CacheFile, "Metadata"));
            Sections = new Dictionary<string, ScenarioSection>();
            Hierarchy = new ObservableCollection<SceneNodeModel>();
            Items = new ObservableCollection<ScenarioListItem>();

            Bsps = new ObservableCollection<TagReference>();
            Skies = new ObservableCollection<TagReference>();
            ObjectNames = new List<string>();
            Palettes = new Dictionary<string, PaletteDefinition>();
            StartingPositions = new ObservableCollection<StartPosition>();
            TriggerVolumes = new ObservableCollection<TriggerVolume>();

            using (var reader = CreateReader())
            {
                LoadSections(reader);

                SquadHierarchy = new AiSquadHierarchy(this);
                ReadSquadHierarchy(reader);

                //populate hierarchy tree
                var doc = new XmlDocument();
                doc.LoadXml(Properties.Resources.NodeHierarchy);
                Hierarchy.AddRange(XmlToNodes(null, doc.DocumentElement));
                Hierarchy[0].IsExpanded = true;

                ReadBsps(reader);
                ReadSkies(reader);
                ReadObjectNames(reader);
                ReadPalettes(reader);
                ReadPlacements(reader);
                ReadStartPositions(reader);
                ReadTriggerVolumes(reader);
            }

            ComponentManagers = new List<ComponentManager>();
            ComponentManagers.Add(new MissionComponentManager(this));
            ComponentManagers.Add(new PaletteComponentManager(this));
            ComponentManagers.Add(new TerrainComponentManager(this));
            ComponentManagers.Add(new StartPositionComponentManager(this));
            ComponentManagers.Add(new TriggerVolumeComponentManager(this));
            ComponentManagers.Add(new AiComponentManager(this));

            IsBusy = false;
        }

        internal T GetComponent<T>() where T : ComponentManager => ComponentManagers.OfType<T>().FirstOrDefault();

        internal ComponentManager GetNodeTypeHandler(NodeType handlesNodeType) => ComponentManagers.FirstOrDefault(c => c.HandlesNodeType(handlesNodeType));

        private string GetXmlData(ICacheFile cache, string suffix)
        {
            string prefix;
            switch (cache.CacheType)
            {
                case CacheType.Halo3Retail:
                case CacheType.MccHalo3:
                    prefix = "Halo3";
                    break;
                case CacheType.Halo3ODST:
                    prefix = "Halo3ODST";
                    break;
                case CacheType.MccHalo3ODST:
                    prefix = "MccHalo3ODST";
                    break;
                case CacheType.HaloReachRetail:
                    prefix = "HaloReach";
                    break;
                case CacheType.MccHaloReach:
                case CacheType.MccHaloReachU3:
                    prefix = "MccHaloReach";
                    break;
                default: throw new NotSupportedException();
            }

            return (string)typeof(Properties.Resources)
                .GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .First(p => p.Name == $"{prefix}{suffix}")
                .GetValue(null);
        }

        #region Initialisation
        private IEnumerable<SceneNodeModel> XmlToNodes(SceneNodeModel parent, XmlNode xml)
        {
            var header = xml.GetStringAttribute("header");
            var type = xml.GetEnumAttribute<NodeType>("type") ?? NodeType.None;
            var visible = xml.GetBoolAttribute("visible") ?? true;
            var visibility = visible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

            var minGame = xml.GetEnumAttribute<CacheType>("minGame");
            var maxGame = xml.GetEnumAttribute<CacheType>("maxGame");

            if (ScenarioTag.CacheFile.CacheType < minGame || ScenarioTag.CacheFile.CacheType >= maxGame)
                return Enumerable.Empty<SceneNodeModel>();

            var nodeInstance = new SceneNodeModel(header, type) { Visibility = visibility, Tag = parent?.Tag };

            IEnumerable<SceneNodeModel> nodes;
            if (xml.Name == "placeholder" && parent != null)
                nodes = GetPlaceholderInstances(parent, nodeInstance).ToList();
            else nodes = Enumerable.Repeat(nodeInstance, 1);

            foreach (var node in nodes)
            {
                var children = xml.ChildNodes.OfType<XmlNode>().SelectMany(n => XmlToNodes(node, n));
                foreach (var c in children)
                    node.Items.Add(c);
            }

            return nodes;
        }

        private int OffsetById(XmlNode node, string fieldId)
        {
            return node.SelectSingleNode($"*[@id='{fieldId}']")?.GetIntAttribute("offset") ?? -1;
        }

        private void LoadSections(EndianReader reader)
        {
            Xml.LoadXml(GetXmlData(ScenarioTag.CacheFile, "Scenario"));

            foreach (XmlNode n in Xml.SelectNodes("/scenario/section[@name]"))
            {
                var section = new ScenarioSection { Node = n };
                section.Name = n.GetStringAttribute("name");
                section.Offset = n.GetIntAttribute("offset") ?? 0;
                section.BlockSize = n.GetIntAttribute("elementSize", "entrySize", "size") ?? 0;

                Sections.Add(section.Name, section);
            }

            foreach (var section in Sections.Values.Where(s => s.BlockSize > 0))
            {
                reader.Seek(RootAddress + section.Offset, SeekOrigin.Begin);
                section.TagBlock = reader.ReadObject<TagBlock>();
            }
        }

        private void ReadSquadHierarchy(EndianReader reader)
        {
            var blockNode = SquadHierarchy.AiNodes[AiSection.SquadGroups];
            var blockRef = new BlockReference(blockNode, reader, RootAddress);

            var name = OffsetById(blockNode, FieldId.Name);

            //squad groups
            for (int i = 0; i < blockRef.TagBlock.Count; i++)
            {
                var group = new AiNamedBlock(blockRef, i);
                var baseAddress = blockRef.TagBlock.Pointer.Address + blockRef.BlockSize * i;

                reader.Seek(baseAddress + name, SeekOrigin.Begin);
                group.Name = reader.ReadNullTerminatedString(32);

                SquadHierarchy.SquadGroups.Add(group);
            }

            ReadZones(reader);
        }

        private void ReadZones(EndianReader reader)
        {
            var squads = ReadSquads(reader, RootAddress);

            SquadHierarchy.DefaultZone.Squads.AddRange(squads.Where(e => e.ZoneIndex == -1));

            var blockNode = SquadHierarchy.AiNodes[AiSection.Zones];
            var blockRef = new BlockReference(blockNode, reader, RootAddress);

            var name = OffsetById(blockNode, FieldId.Name);

            for (int i = 0; i < blockRef.TagBlock.Count; i++)
            {
                var zone = new AiZone(blockRef, i);
                var baseAddress = blockRef.TagBlock.Pointer.Address + blockRef.BlockSize * i;

                reader.Seek(baseAddress + name, SeekOrigin.Begin);
                zone.Name = reader.ReadNullTerminatedString(32);

                ReadFiringPositions(reader, zone, baseAddress);
                ReadAreas(reader, zone, baseAddress);
                zone.Squads.AddRange(squads.Where(e => e.ZoneIndex == i));

                SquadHierarchy.Zones.Add(zone);
            }
        }

        private void ReadFiringPositions(EndianReader reader, AiZone owner, long rootAddress)
        {
            var blockNode = SquadHierarchy.AiNodes[AiSection.FiringPositions];
            var blockRef = new BlockReference(blockNode, reader, rootAddress);

            var position = OffsetById(blockNode, FieldId.Position);

            for (int i = 0; i < blockRef.TagBlock.Count; i++)
            {
                var fpos = new AiFiringPosition(this, owner, blockRef, i);
                var baseAddress = blockRef.TagBlock.Pointer.Address + blockRef.BlockSize * i;

                reader.Seek(baseAddress + position, SeekOrigin.Begin);
                fpos.Position = reader.ReadObject<RealVector3D>();

                owner.FiringPositions.Add(fpos);
            }
        }

        private void ReadAreas(EndianReader reader, AiZone owner, long rootAddress)
        {
            var blockNode = SquadHierarchy.AiNodes[AiSection.Areas];
            var blockRef = new BlockReference(blockNode, reader, rootAddress);

            var name = OffsetById(blockNode, FieldId.Name);
            var position = OffsetById(blockNode, FieldId.Position);

            for (int i = 0; i < blockRef.TagBlock.Count; i++)
            {
                var area = new AiArea(this, owner, blockRef, i);
                var baseAddress = blockRef.TagBlock.Pointer.Address + blockRef.BlockSize * i;

                reader.Seek(baseAddress + name, SeekOrigin.Begin);
                area.Name = reader.ReadNullTerminatedString(32);

                reader.Seek(baseAddress + position, SeekOrigin.Begin);
                area.Position = reader.ReadObject<RealVector3D>();

                owner.Areas.Add(area);
            }
        }

        private List<AiSquad> ReadSquads(EndianReader reader, long rootAddress)
        {
            var results = new List<AiSquad>();

            var blockNode = SquadHierarchy.AiNodes[AiSection.Squads];
            var blockRef = new BlockReference(blockNode, reader, rootAddress);

            var name = OffsetById(blockNode, FieldId.Name);
            var parentIndex = OffsetById(blockNode, FieldId.ParentIndex);

            for (int i = 0; i < blockRef.TagBlock.Count; i++)
            {
                var squad = new AiSquad(blockRef, i);
                var baseAddress = blockRef.TagBlock.Pointer.Address + blockRef.BlockSize * i;

                reader.Seek(baseAddress + name, SeekOrigin.Begin);
                squad.Name = reader.ReadNullTerminatedString(32);

                reader.Seek(baseAddress + parentIndex, SeekOrigin.Begin);
                squad.ZoneIndex = reader.ReadInt16();

                if (ScenarioTag.CacheFile.CacheType < CacheType.Halo3ODST)
                    ReadEncounters(reader, squad, baseAddress);
                else
                {
                    squad.GroupStartLocations.AddRange(ReadStartingLocations(reader, baseAddress, AiSection.GroupSTartLocations));
                    squad.SoloStartLocations.AddRange(ReadStartingLocations(reader, baseAddress, AiSection.SoloStartLocations));
                }

                results.Add(squad);
            }

            return results;
        }

        private void ReadEncounters(EndianReader reader, AiSquad owner, long rootAddress)
        {
            var blockNode = SquadHierarchy.AiNodes[AiSection.Encounters];
            var blockRef = new BlockReference(blockNode, reader, rootAddress);

            for (int i = 0; i < blockRef.TagBlock.Count; i++)
            {
                var enc = new AiEncounter(blockRef, i) { Name = $"Encounter {i}" };
                var baseAddress = blockRef.TagBlock.Pointer.Address + blockRef.BlockSize * i;

                enc.StartingLocations.AddRange(ReadStartingLocations(reader, baseAddress, AiSection.StartLocations));

                owner.Encounters.Add(enc);
            }
        }

        private List<AiStartingLocation> ReadStartingLocations(EndianReader reader, long rootAddress, string sectionKey)
        {
            var result = new List<AiStartingLocation>();

            var blockNode = SquadHierarchy.AiNodes[sectionKey];
            var blockRef = new BlockReference(blockNode, reader, rootAddress);

            var name = OffsetById(blockNode, FieldId.Name);
            var position = OffsetById(blockNode, FieldId.Position);
            var rotation = OffsetById(blockNode, FieldId.Rotation);

            for (int i = 0; i < blockRef.TagBlock.Count; i++)
            {
                var loc = new AiStartingLocation(this, blockRef, i, sectionKey);
                var baseAddress = blockRef.TagBlock.Pointer.Address + blockRef.BlockSize * i;

                reader.Seek(baseAddress + name, SeekOrigin.Begin);
                loc.Name = reader.ReadObject<StringId>();

                reader.Seek(baseAddress + position, SeekOrigin.Begin);
                loc.Position = reader.ReadObject<RealVector3D>();

                reader.Seek(baseAddress + rotation, SeekOrigin.Begin);
                loc.Rotation = reader.ReadObject<RealVector3D>();

                result.Add(loc);
            }

            return result;
        }

        private void ReadBsps(EndianReader reader)
        {
            var section = Sections[Section.StructureBsps];
            var refOffset = OffsetById(section.Node, FieldId.TagReference);

            for (int i = 0; i < section.TagBlock.Count; i++)
            {
                reader.Seek(section.TagBlock.Pointer.Address + section.BlockSize * i + refOffset, SeekOrigin.Begin);
                Bsps.Add(reader.ReadObject<TagReference>());
            }
        }

        private void ReadSkies(EndianReader reader)
        {
            var section = Sections[Section.Skies];
            var refOffset = OffsetById(section.Node, FieldId.TagReference);

            for (int i = 0; i < section.TagBlock.Count; i++)
            {
                reader.Seek(section.TagBlock.Pointer.Address + section.BlockSize * i + refOffset, SeekOrigin.Begin);
                Skies.Add(reader.ReadObject<TagReference>());
            }
        }

        private void ReadObjectNames(EndianReader reader)
        {
            var section = Sections[Section.ObjectNames];
            var nameOffset = OffsetById(section.Node, FieldId.Name);

            for (int i = 0; i < section.TagBlock.Count; i++)
            {
                reader.Seek(section.TagBlock.Pointer.Address + section.BlockSize * i + nameOffset, SeekOrigin.Begin);
                if (ScenarioTag.CacheFile.CacheType < CacheType.HaloReachBeta)
                    ObjectNames.Add(reader.ReadNullTerminatedString(32));
                else
                    ObjectNames.Add(reader.ReadObject<StringId>());
            }
        }

        private void ReadPalettes(EndianReader reader)
        {
            var section = Sections[Section.Palettes];
            foreach (XmlNode paletteNode in section.Node.SelectNodes("*[@palette]"))
            {
                var paletteDef = new PaletteDefinition
                {
                    Name = paletteNode.GetStringAttribute("palette"),
                    PaletteNode = paletteNode
                };

                var blockRef = paletteDef.PaletteBlockRef = new BlockReference(paletteNode, reader, RootAddress);
                var refOffset = OffsetById(paletteNode, FieldId.TagReference);
                for (int i = 0; i < blockRef.TagBlock.Count; i++)
                {
                    reader.Seek(blockRef.TagBlock.Pointer.Address + blockRef.BlockSize * i + refOffset, SeekOrigin.Begin);
                    paletteDef.Palette.Add(reader.ReadObject<TagReference>());
                }

                Palettes.Add(paletteDef.Name, paletteDef);
            }
        }

        private void ReadPlacements(EndianReader reader)
        {
            var section = Sections[Section.Placements];
            foreach (XmlNode placementNode in section.Node.SelectNodes("*[@placements]"))
            {
                var paletteName = placementNode.GetStringAttribute("placements");
                var paletteDef = Palettes[paletteName];
                paletteDef.PlacementsNode = placementNode;

                var blockRef = paletteDef.PlacementBlockRef = new BlockReference(placementNode, reader, RootAddress);

                var paletteIndex = OffsetById(placementNode, FieldId.PaletteIndex);
                var nameIndex = OffsetById(placementNode, FieldId.NameIndex);
                var position = OffsetById(placementNode, FieldId.Position);
                var rotation = OffsetById(placementNode, FieldId.Rotation);
                var qrotation = OffsetById(placementNode, FieldId.QRotation);
                var scale = OffsetById(placementNode, FieldId.Scale);
                var variant = OffsetById(placementNode, FieldId.Variant);

                for (int i = 0; i < blockRef.TagBlock.Count; i++)
                {
                    var placement = new ObjectPlacement(this, paletteName);
                    var baseAddress = blockRef.TagBlock.Pointer.Address + blockRef.BlockSize * i;

                    reader.Seek(baseAddress + paletteIndex, SeekOrigin.Begin);
                    placement.PaletteIndex = reader.ReadInt16();

                    if (nameIndex >= 0)
                    {
                        reader.Seek(baseAddress + nameIndex, SeekOrigin.Begin);
                        placement.NameIndex = reader.ReadInt16();
                    }
                    else placement.NameIndex = -1;

                    reader.Seek(baseAddress + position, SeekOrigin.Begin);
                    placement.Position = reader.ReadObject<RealVector3D>();

                    if (rotation >= 0)
                    {
                        reader.Seek(baseAddress + rotation, SeekOrigin.Begin);
                        placement.Rotation = reader.ReadObject<RealVector3D>();
                    }

                    if (qrotation >= 0)
                    {
                        reader.Seek(baseAddress + qrotation, SeekOrigin.Begin);
                        placement.QRotation = reader.ReadObject<RealVector4D>();
                    }

                    reader.Seek(baseAddress + scale, SeekOrigin.Begin);
                    placement.Scale = reader.ReadSingle();

                    if (variant >= 0)
                    {
                        reader.Seek(baseAddress + variant, SeekOrigin.Begin);
                        placement.Variant = reader.ReadObject<StringId>();
                    }

                    paletteDef.Placements.Add(placement);
                }
            }
        }

        private void ReadStartPositions(EndianReader reader)
        {
            var section = Sections[Section.StartPositions];
            var position = OffsetById(section.Node, FieldId.Position);
            var orientation = OffsetById(section.Node, FieldId.Orientation);

            for (int i = 0; i < section.TagBlock.Count; i++)
            {
                var startPos = new StartPosition(this);
                var baseAddress = section.TagBlock.Pointer.Address + section.BlockSize * i;

                reader.Seek(baseAddress + position, SeekOrigin.Begin);
                startPos.Position = reader.ReadObject<RealVector3D>();

                reader.Seek(baseAddress + orientation, SeekOrigin.Begin);
                startPos.Orientation = reader.ReadObject<RealVector2D>();

                StartingPositions.Add(startPos);
            }
        }

        private void ReadTriggerVolumes(EndianReader reader)
        {
            var section = Sections[Section.TriggerVolumes];
            var name = OffsetById(section.Node, FieldId.Name);
            var forward = OffsetById(section.Node, FieldId.ForwardVector);
            var up = OffsetById(section.Node, FieldId.UpVector);
            var position = OffsetById(section.Node, FieldId.Position);
            var size = OffsetById(section.Node, FieldId.Size);

            for (int i = 0; i < section.TagBlock.Count; i++)
            {
                var volume = new TriggerVolume(this);
                var baseAddress = section.TagBlock.Pointer.Address + section.BlockSize * i;

                reader.Seek(baseAddress + name, SeekOrigin.Begin);
                volume.Name = reader.ReadObject<StringId>();

                reader.Seek(baseAddress + forward, SeekOrigin.Begin);
                volume.ForwardVector = reader.ReadObject<RealVector3D>();

                reader.Seek(baseAddress + up, SeekOrigin.Begin);
                volume.UpVector = reader.ReadObject<RealVector3D>();

                reader.Seek(baseAddress + position, SeekOrigin.Begin);
                volume.Position = reader.ReadObject<RealVector3D>();

                reader.Seek(baseAddress + size, SeekOrigin.Begin);
                volume.Size = reader.ReadObject<RealVector3D>();

                TriggerVolumes.Add(volume);
            }
        }

        private IEnumerable<SceneNodeModel> GetPlaceholderInstances(SceneNodeModel parent, SceneNodeModel placeholder)
        {
            if (placeholder.NodeType == NodeType.AiDefaultZoneItem)
                return Enumerable.Repeat(SquadHierarchy.DefaultZone, 1).Select(z => new SceneNodeModel(z.Name, NodeType.AiDefaultZoneItem) { Tag = z, IconType = 1 });
            if (placeholder.NodeType == NodeType.AiZoneItem)
                return SquadHierarchy.Zones.Select(z => new SceneNodeModel(z.Name, NodeType.AiZoneItem) { Tag = z, IconType = 1 });
            else if (placeholder.NodeType == NodeType.AiSquadItem)
                return (parent.Tag as AiZone).Squads.Select(e => new SceneNodeModel(e.Name, NodeType.AiSquadItem) { Tag = e, IconType = 1 });
            else if (placeholder.NodeType == NodeType.AiEncounterItem)
                return (parent.Tag as AiSquad).Encounters.Select(s => new SceneNodeModel(s.Name, NodeType.AiEncounterItem) { Tag = s, IconType = 1 });
            else return new List<SceneNodeModel> { placeholder };
        }
        #endregion

        private void DisplayItems()
        {
            SelectedItemIndex = -1;
            Items.Clear();

            if (SelectedNodeType == NodeType.None)
                return;

            var handler = GetNodeTypeHandler(SelectedNodeType);
            if (handler != null)
                Items.AddRange(handler.GetListItems(selectedNode));

            HierarchyView.ShowCurrentSelection();
        }

        private void OnSelectedNodeChanged()
        {
            PropertyView?.ShowProperties(SelectedNode, -1);
            RenderView?.SelectPalette(SelectedNode);
            DisplayItems();
        }

        private void OnSelectedItemChanged()
        {
            HierarchyView.ShowCurrentSelection();
            PropertyView?.ShowProperties(SelectedNode, SelectedItemIndex);
            RenderView?.SelectObject(SelectedNode, SelectedItemIndex);
        }

        public EndianReader CreateReader()
        {
            var reader = ScenarioTag.CacheFile.CreateReader(ScenarioTag.CacheFile.DefaultAddressTranslator, MetadataStream, true);
            var expander = (ScenarioTag.CacheFile as IMccCacheFile)?.PointerExpander;
            if (expander != null)
                reader.RegisterInstance(expander);

            return reader;
        }

        public EndianWriter CreateWriter() => new EndianWriter(MetadataStream, ScenarioTag.CacheFile.ByteOrder, new UTF8Encoding(), true);
    }

    public class PaletteDefinition
    {
        public string Name { get; set; }
        public XmlNode PaletteNode { get; set; }
        public XmlNode PlacementsNode { get; set; }
        public BlockReference PaletteBlockRef { get; set; }
        public BlockReference PlacementBlockRef { get; set; }
        public ObservableCollection<TagReference> Palette { get; set; }
        public ObservableCollection<ObjectPlacement> Placements { get; set; }

        public PaletteDefinition()
        {
            Palette = new ObservableCollection<TagReference>();
            Placements = new ObservableCollection<ObjectPlacement>();
        }
    }

    public class BlockReference
    {
        public BlockReference()
        { }

        public BlockReference(XmlNode node, EndianReader reader, long baseAddress)
        {
            var Offset = node.GetIntAttribute("offset") ?? 0;
            BlockSize = node.GetIntAttribute("elementSize", "entrySize", "size") ?? 0;

            reader.Seek(baseAddress + Offset, SeekOrigin.Begin);
            TagBlock = reader.ReadObject<TagBlock>();
        }

        //public int Offset { get; set; }
        public int BlockSize { get; set; }
        public TagBlock TagBlock { get; set; }
    }

    public class ScenarioSection
    {
        public XmlNode Node { get; set; }
        public string Name { get; set; }
        public int Offset { get; set; }
        public int BlockSize { get; set; }
        public TagBlock TagBlock { get; set; }

        public override string ToString() => Name;
    }
}
