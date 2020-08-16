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

namespace Reclaimer.Models
{
    public class ScenarioModel : BindableBase
    {
        public event EventHandler SelectedNodeChanged;
        public event EventHandler SelectedItemChanged;

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

        public Dictionary<string, ScenarioSection> Sections { get; }
        public ObservableCollection<TreeItemModel> Hierarchy { get; }
        public ObservableCollection<string> Items { get; }

        public List<string> ObjectNames { get; }
        public Dictionary<string, PaletteDefinition> Palettes { get; }

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
            Sections = new Dictionary<string, ScenarioSection>();
            Hierarchy = new ObservableCollection<TreeItemModel>();
            Items = new ObservableCollection<string>();

            ObjectNames = new List<string>();
            Palettes = new Dictionary<string, PaletteDefinition>();

            LoadSections();
            LoadHierarchy();
            LoadObjectNames();
            LoadPalettes();
            LoadPlacements();
        }

        private void LoadSections()
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
        }

        private void LoadHierarchy()
        {
            var doc = new XmlDocument();
            doc.LoadXml(Properties.Resources.NodeHierarchy);

            Hierarchy.Add(XmlToNode(doc.DocumentElement));
            Hierarchy[0].IsExpanded = true;
        }

        private void LoadObjectNames()
        {
            var reader = ScenarioTag.CacheFile.CreateReader(ScenarioTag.CacheFile.DefaultAddressTranslator);

            var section = Sections["objectnames"];
            var nameOffset = section.Node.SelectSingleNode("*[@id='name']").GetIntAttribute("offset") ?? 0;

            reader.Seek(ScenarioTag.MetaPointer.Address + section.Offset, System.IO.SeekOrigin.Begin);
            var tagBlock = reader.ReadObject<TagBlock>();
            for (int i = 0; i < tagBlock.Count; i++)
            {
                reader.Seek(tagBlock.Pointer.Address + section.BlockSize * i + nameOffset, System.IO.SeekOrigin.Begin);
                ObjectNames.Add(reader.ReadNullTerminatedString(32));
            }
        }

        private void LoadPalettes()
        {
            var reader = ScenarioTag.CacheFile.CreateReader(ScenarioTag.CacheFile.DefaultAddressTranslator);

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

                reader.Seek(ScenarioTag.MetaPointer.Address + blockRef.Offset, System.IO.SeekOrigin.Begin);
                var tagBlock = paletteDef.PaletteBlockRef.TagBlock = reader.ReadObject<TagBlock>();

                var refOffset = paletteNode.SelectSingleNode("*[@id='tagreference']").GetIntAttribute("offset") ?? 0;
                for (int i = 0; i < tagBlock.Count; i++)
                {
                    reader.Seek(tagBlock.Pointer.Address + blockRef.BlockSize * i + refOffset, System.IO.SeekOrigin.Begin);
                    paletteDef.Palette.Add(reader.ReadObject<TagReference>());
                }

                Palettes.Add(paletteDef.Name, paletteDef);
            }
        }

        private void LoadPlacements()
        {
            var reader = ScenarioTag.CacheFile.CreateReader(ScenarioTag.CacheFile.DefaultAddressTranslator);

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

                var paletteIndex = placementNode.SelectSingleNode("*[@id='paletteindex']").GetIntAttribute("offset") ?? 0;
                var nameIndex = placementNode.SelectSingleNode("*[@id='nameindex']").GetIntAttribute("offset") ?? 0;
                var position = placementNode.SelectSingleNode("*[@id='position']").GetIntAttribute("offset") ?? 0;
                var rotation = placementNode.SelectSingleNode("*[@id='rotation']").GetIntAttribute("offset") ?? 0;
                var scale = placementNode.SelectSingleNode("*[@id='scale']").GetIntAttribute("offset") ?? 0;
                var variant = placementNode.SelectSingleNode("*[@id='variant']")?.GetIntAttribute("offset");

                reader.Seek(ScenarioTag.MetaPointer.Address + blockRef.Offset, System.IO.SeekOrigin.Begin);
                var tagBlock = paletteDef.PlacementBlockRef.TagBlock = reader.ReadObject<TagBlock>();

                for (int i = 0; i < tagBlock.Count; i++)
                {
                    var placement = new ObjectPlacement();
                    var baseAddress = tagBlock.Pointer.Address + blockRef.BlockSize * i;

                    reader.Seek(baseAddress + paletteIndex, System.IO.SeekOrigin.Begin);
                    placement.PaletteIndex = reader.ReadInt16();

                    reader.Seek(baseAddress + nameIndex, System.IO.SeekOrigin.Begin);
                    placement.NameIndex = reader.ReadInt16();

                    reader.Seek(baseAddress + position, System.IO.SeekOrigin.Begin);
                    placement.Position = reader.ReadObject<RealVector3D>();

                    reader.Seek(baseAddress + rotation, System.IO.SeekOrigin.Begin);
                    placement.Rotation = reader.ReadObject<RealVector3D>();

                    reader.Seek(baseAddress + scale, System.IO.SeekOrigin.Begin);
                    placement.Scale = reader.ReadSingle();

                    if (variant.HasValue)
                    {
                        reader.Seek(baseAddress + variant.Value, System.IO.SeekOrigin.Begin);
                        placement.Variant = reader.ReadObject<StringId>();
                    }

                    paletteDef.Placements.Add(placement);
                }
            }
        }

        private void LoadItems()
        {
            SelectedItemIndex = -1;
            Items.Clear();

            if (selectedNode == null)
                return;

            var nodeType = (NodeType)SelectedNode.Tag;

            var paletteKey = PaletteType.FromNodeType(nodeType);
            if (paletteKey != null)
            {
                LoadPaletteItems(Palettes[paletteKey]);
                return;
            }

            //load other stuff like sandbox items and trigger volumes
        }

        private void LoadPaletteItems(PaletteDefinition palette)
        {
            for (int i = 0; i < palette.Placements.Count; i++)
            {
                var placement = palette.Placements[i];
                if (placement.PaletteIndex < 0 || placement.PaletteIndex >= palette.Palette.Count)
                {
                    Items.Add("<invalid>");
                    continue;
                }

                if (placement.NameIndex >= 0)
                    Items.Add(ObjectNames[placement.NameIndex]);
                else Items.Add(palette.Palette[placement.PaletteIndex].Tag.FileName());
            }
        }

        private void OnSelectedNodeChanged()
        {
            LoadItems();
            SelectedNodeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnSelectedItemChanged()
        {
            SelectedItemChanged?.Invoke(this, EventArgs.Empty);
        }
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

    public struct ScenarioSection
    {
        public XmlNode Node { get; set; }
        public string Name { get; set; }
        public int Offset { get; set; }
        public int BlockSize { get; set; }
    }

    public struct ObjectPlacement
    {
        public int PaletteIndex { get; set; }
        public int NameIndex { get; set; }
        public RealVector3D Position { get; set; }
        public RealVector3D Rotation { get; set; }
        public float Scale { get; set; }
        public StringId Variant { get; set; }
    }
}
