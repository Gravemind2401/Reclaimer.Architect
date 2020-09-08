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

namespace Reclaimer.Models
{
    public class ScenarioModel : BindableBase
    {
        private static TreeItemModel XmlToNode(XmlNode xml)
        {
            var header = xml.GetStringAttribute("header");
            var type = xml.GetEnumAttribute<NodeType>("type") ?? NodeType.None;

            var node = new TreeItemModel(header) { Tag = type };

            var children = xml.ChildNodes.OfType<XmlNode>().Select(n => XmlToNode(n));
            foreach (var c in children)
                node.Items.Add(c);

            return node;
        }

        public IIndexItem ScenarioTag { get; }

        public TransactionStream Transaction { get; }
        public Dictionary<string, ScenarioSection> Sections { get; }
        public ObservableCollection<TreeItemModel> Hierarchy { get; }
        public ObservableCollection<string> Items { get; }

        public ObservableCollection<TagReference> Bsps { get; }
        public ObservableCollection<TagReference> Skies { get; }
        public List<string> ObjectNames { get; }
        public Dictionary<string, PaletteDefinition> Palettes { get; }
        public ObservableCollection<TriggerVolume> TriggerVolumes { get; }

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

        private TreeItemModel selectedNode;
        public TreeItemModel SelectedNode
        {
            get { return selectedNode; }
            set
            {
                if (SetProperty(ref selectedNode, value))
                    OnSelectedNodeChanged();
            }
        }

        public NodeType SelectedNodeType => SelectedNode?.Tag as NodeType? ?? NodeType.None;

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
            ScenarioTag = item;
            Transaction = new TransactionStream(new MemoryStream());
            Sections = new Dictionary<string, ScenarioSection>();
            Hierarchy = new ObservableCollection<TreeItemModel>();
            Items = new ObservableCollection<string>();

            Bsps = new ObservableCollection<TagReference>();
            Skies = new ObservableCollection<TagReference>();
            ObjectNames = new List<string>();
            Palettes = new Dictionary<string, PaletteDefinition>();
            TriggerVolumes = new ObservableCollection<TriggerVolume>();

            using (var reader = ScenarioTag.CacheFile.CreateReader(ScenarioTag.CacheFile.DefaultAddressTranslator))
            {
                LoadSections(reader);
                LoadHierarchy();
                ReadBsps(reader);
                ReadSkies(reader);
                ReadObjectNames(reader);
                ReadPalettes(reader);
                ReadPlacements(reader);
                ReadTriggerVolumes(reader);
            }
        }

        #region Initialisation
        private void LoadSections(EndianReader reader)
        {
            var doc = new XmlDocument();
            doc.LoadXml(Properties.Resources.Halo3Scenario);

            foreach (XmlNode n in doc.SelectNodes("/scenario/section[@name]"))
            {
                var section = new ScenarioSection { Node = n };
                section.Name = n.GetStringAttribute("name");
                section.Offset = n.GetIntAttribute("offset") ?? 0;
                section.BlockSize = n.GetIntAttribute("elementSize", "entrySize", "size") ?? 0;

                Sections.Add(section.Name, section);
            }

            foreach (var section in Sections.Values.Where(s => s.BlockSize > 0))
            {
                reader.Seek(ScenarioTag.MetaPointer.Address + section.Offset, SeekOrigin.Begin);
                section.TagBlock = reader.ReadObject<TagBlock>();
            }
        }

        private void LoadHierarchy()
        {
            var doc = new XmlDocument();
            doc.LoadXml(Properties.Resources.NodeHierarchy);

            Hierarchy.Add(XmlToNode(doc.DocumentElement));
            Hierarchy[0].IsExpanded = true;
        }

        private void ReadBsps(EndianReader reader)
        {
            var section = Sections["structurebsps"];
            var refOffset = section.Node.SelectSingleNode($"*[@id='{FieldId.TagReference}']").GetIntAttribute("offset") ?? 0;

            for (int i = 0; i < section.TagBlock.Count; i++)
            {
                reader.Seek(section.TagBlock.Pointer.Address + section.BlockSize * i + refOffset, SeekOrigin.Begin);
                Bsps.Add(reader.ReadObject<TagReference>());
            }
        }

        private void ReadSkies(EndianReader reader)
        {
            var section = Sections["skies"];
            var refOffset = section.Node.SelectSingleNode($"*[@id='{FieldId.TagReference}']").GetIntAttribute("offset") ?? 0;

            for (int i = 0; i < section.TagBlock.Count; i++)
            {
                reader.Seek(section.TagBlock.Pointer.Address + section.BlockSize * i + refOffset, SeekOrigin.Begin);
                Skies.Add(reader.ReadObject<TagReference>());
            }
        }

        private void ReadObjectNames(EndianReader reader)
        {
            var section = Sections["objectnames"];
            var nameOffset = section.Node.SelectSingleNode($"*[@id='{FieldId.Name}']").GetIntAttribute("offset") ?? 0;

            for (int i = 0; i < section.TagBlock.Count; i++)
            {
                reader.Seek(section.TagBlock.Pointer.Address + section.BlockSize * i + nameOffset, SeekOrigin.Begin);
                ObjectNames.Add(reader.ReadNullTerminatedString(32));
            }
        }

        private void ReadPalettes(EndianReader reader)
        {
            var section = Sections["palettes"];
            foreach (XmlNode paletteNode in section.Node.SelectNodes("*[@palette]"))
            {
                var paletteDef = new PaletteDefinition
                {
                    Name = paletteNode.GetStringAttribute("palette"),
                    PaletteNode = paletteNode
                };

                var blockRef = paletteDef.PaletteBlockRef = new BlockReference
                {
                    Offset = paletteNode.GetIntAttribute("offset") ?? 0,
                    BlockSize = paletteNode.GetIntAttribute("elementSize", "entrySize", "size") ?? 0
                };

                reader.Seek(ScenarioTag.MetaPointer.Address + blockRef.Offset, SeekOrigin.Begin);
                var tagBlock = paletteDef.PaletteBlockRef.TagBlock = reader.ReadObject<TagBlock>();

                var refOffset = paletteNode.SelectSingleNode($"*[@id='{FieldId.TagReference}']").GetIntAttribute("offset") ?? 0;
                for (int i = 0; i < tagBlock.Count; i++)
                {
                    reader.Seek(tagBlock.Pointer.Address + blockRef.BlockSize * i + refOffset, SeekOrigin.Begin);
                    paletteDef.Palette.Add(reader.ReadObject<TagReference>());
                }

                Palettes.Add(paletteDef.Name, paletteDef);
            }
        }

        private void ReadPlacements(EndianReader reader)
        {
            var section = Sections["placements"];
            foreach (XmlNode placementNode in section.Node.SelectNodes("*[@placements]"))
            {
                var paletteName = placementNode.GetStringAttribute("placements");
                var paletteDef = Palettes[paletteName];
                paletteDef.PlacementsNode = placementNode;

                var blockRef = paletteDef.PlacementBlockRef = new BlockReference
                {
                    Offset = placementNode.GetIntAttribute("offset") ?? 0,
                    BlockSize = placementNode.GetIntAttribute("elementSize", "entrySize", "size") ?? 0
                };

                var paletteIndex = placementNode.SelectSingleNode($"*[@id='{FieldId.PaletteIndex}']").GetIntAttribute("offset") ?? 0;
                var nameIndex = placementNode.SelectSingleNode($"*[@id='{FieldId.NameIndex}']").GetIntAttribute("offset") ?? 0;
                var position = placementNode.SelectSingleNode($"*[@id='{FieldId.Position}']").GetIntAttribute("offset") ?? 0;
                var rotation = placementNode.SelectSingleNode($"*[@id='{FieldId.Rotation}']").GetIntAttribute("offset") ?? 0;
                var scale = placementNode.SelectSingleNode($"*[@id='{FieldId.Scale}']").GetIntAttribute("offset") ?? 0;
                var variant = placementNode.SelectSingleNode($"*[@id='{FieldId.Variant}']")?.GetIntAttribute("offset");

                reader.Seek(ScenarioTag.MetaPointer.Address + blockRef.Offset, SeekOrigin.Begin);
                var tagBlock = paletteDef.PlacementBlockRef.TagBlock = reader.ReadObject<TagBlock>();

                for (int i = 0; i < tagBlock.Count; i++)
                {
                    var placement = new ObjectPlacement(this, paletteName);
                    var baseAddress = tagBlock.Pointer.Address + blockRef.BlockSize * i;

                    reader.Seek(baseAddress + paletteIndex, SeekOrigin.Begin);
                    placement.PaletteIndex = reader.ReadInt16();

                    reader.Seek(baseAddress + nameIndex, SeekOrigin.Begin);
                    placement.NameIndex = reader.ReadInt16();

                    reader.Seek(baseAddress + position, SeekOrigin.Begin);
                    placement.Position = reader.ReadObject<RealVector3D>();

                    reader.Seek(baseAddress + rotation, SeekOrigin.Begin);
                    placement.Rotation = reader.ReadObject<RealVector3D>();

                    reader.Seek(baseAddress + scale, SeekOrigin.Begin);
                    placement.Scale = reader.ReadSingle();

                    if (variant.HasValue)
                    {
                        reader.Seek(baseAddress + variant.Value, SeekOrigin.Begin);
                        placement.Variant = reader.ReadObject<StringId>();
                    }

                    paletteDef.Placements.Add(placement);
                }
            }
        }

        private void ReadTriggerVolumes(EndianReader reader)
        {
            var section = Sections["triggervolumes"];
            var name = section.Node.SelectSingleNode($"*[@id='{FieldId.Name}']").GetIntAttribute("offset") ?? 0;
            var position = section.Node.SelectSingleNode($"*[@id='{FieldId.Position}']").GetIntAttribute("offset") ?? 0;
            var size = section.Node.SelectSingleNode($"*[@id='{FieldId.Size}']").GetIntAttribute("offset") ?? 0;

            for (int i = 0; i < section.TagBlock.Count; i++)
            {
                var volume = new TriggerVolume(this);
                var baseAddress = section.TagBlock.Pointer.Address + section.BlockSize * i;

                reader.Seek(baseAddress + name, SeekOrigin.Begin);
                volume.Name = reader.ReadObject<StringId>();

                reader.Seek(baseAddress + position, SeekOrigin.Begin);
                volume.Position = reader.ReadObject<RealVector3D>();

                reader.Seek(baseAddress + size, SeekOrigin.Begin);
                volume.Size = reader.ReadObject<RealVector3D>();

                TriggerVolumes.Add(volume);
            }
        }
        #endregion

        private void DisplayItems()
        {
            SelectedItemIndex = -1;
            Items.Clear();

            if (selectedNode == null || SelectedNodeType == NodeType.None)
                return;

            var paletteKey = PaletteType.FromNodeType(SelectedNodeType);
            if (paletteKey != null)
            {
                foreach (var placement in Palettes[paletteKey].Placements)
                    Items.Add(placement.GetDisplayName());

                return;
            }

            //load other stuff like sandbox items and trigger volumes

            if (SelectedNodeType == NodeType.TriggerVolumes)
            {
                foreach (var volume in TriggerVolumes)
                    Items.Add(volume.Name);
            }
        }

        private void OnSelectedNodeChanged()
        {
            PropertyView?.ShowProperties(SelectedNodeType, -1);
            RenderView?.SelectPalette(SelectedNodeType);
            DisplayItems();
        }

        private void OnSelectedItemChanged()
        {
            PropertyView?.ShowProperties(SelectedNodeType, SelectedItemIndex);
            RenderView?.SelectObject(SelectedNodeType, SelectedItemIndex);
        }

        public EndianWriter CreateWriter() => new EndianWriter(Transaction, ScenarioTag.CacheFile.ByteOrder, new UTF8Encoding(), true);
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
        public int Offset { get; set; }
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

    public class ObjectPlacement : BindableBase
    {
        private readonly ScenarioModel parent;
        private readonly string paletteKey;

        private int paletteIndex;
        public int PaletteIndex
        {
            get { return paletteIndex; }
            set { SetProperty(ref paletteIndex, value, FieldId.PaletteIndex); }
        }

        private int nameIndex;
        public int NameIndex
        {
            get { return nameIndex; }
            set { SetProperty(ref nameIndex, value, FieldId.NameIndex); }
        }

        private RealVector3D position;
        public RealVector3D Position
        {
            get { return position; }
            set { SetProperty(ref position, value, FieldId.Position); }
        }

        private RealVector3D rotation;
        public RealVector3D Rotation
        {
            get { return rotation; }
            set { SetProperty(ref rotation, value, FieldId.Rotation); }
        }

        private float scale;
        public float Scale
        {
            get { return scale; }
            set { SetProperty(ref scale, value, FieldId.Scale); }
        }

        private StringId variant;
        public StringId Variant
        {
            get { return variant; }
            set { SetProperty(ref variant, value, FieldId.Variant); }
        }

        public ObjectPlacement(ScenarioModel parent, string paletteKey)
        {
            this.parent = parent;
            this.paletteKey = paletteKey;
        }

        private bool SetProperty<T>(ref T storage, T value, string fieldId, [CallerMemberName] string propertyName = null)
        {
            if (!base.SetProperty(ref storage, value, propertyName))
                return false;

            var palette = parent.Palettes[paletteKey];
            var block = palette.PlacementBlockRef;
            var index = palette.Placements.IndexOf(this);
            var fieldOffset = palette.PlacementsNode.SelectSingleNode($"*[@id='{fieldId}']").GetIntAttribute("offset") ?? 0;

            using (var writer = parent.CreateWriter())
            {
                writer.Seek(block.TagBlock.Pointer.Address + block.BlockSize * index + fieldOffset, SeekOrigin.Begin);

                if (typeof(T) == typeof(StringId))
                    writer.Write(((StringId)(object)value).Id);
                else writer.WriteObject(value);
            }

            if (parent.PropertyView?.CurrentItem == this)
                parent.PropertyView.SetValue(fieldId, value);

            return true;
        }

        public string GetDisplayName()
        {
            var palette = parent.Palettes[paletteKey];

            if (PaletteIndex < 0 || PaletteIndex >= palette.Palette.Count)
                return "<invalid>";

            if (NameIndex >= 0)
                return parent.ObjectNames[NameIndex];
            else return palette.Palette[PaletteIndex].Tag.FileName();
        }

        public override string ToString() => GetDisplayName();
    }
}
