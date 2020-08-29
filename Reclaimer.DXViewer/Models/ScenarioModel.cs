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

        public ObservableCollection<TagReference> Bsps { get; }
        public ObservableCollection<TagReference> Skies { get; }
        public List<string> ObjectNames { get; }
        public Dictionary<string, PaletteDefinition> Palettes { get; }

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
            Sections = new Dictionary<string, ScenarioSection>();
            Hierarchy = new ObservableCollection<TreeItemModel>();
            Items = new ObservableCollection<string>();

            Bsps = new ObservableCollection<TagReference>();
            Skies = new ObservableCollection<TagReference>();
            ObjectNames = new List<string>();
            Palettes = new Dictionary<string, PaletteDefinition>();

            LoadSections();
            LoadHierarchy();
            ReadBsps();
            ReadSkies();
            ReadObjectNames();
            ReadPalettes();
            ReadPlacements();
        }

        #region Initialisation
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

        private void ReadBsps()
        {
            var reader = ScenarioTag.CacheFile.CreateReader(ScenarioTag.CacheFile.DefaultAddressTranslator);

            var section = Sections["structurebsps"];
            var refOffset = section.Node.SelectSingleNode("*[@id='tagreference']").GetIntAttribute("offset") ?? 0;

            reader.Seek(ScenarioTag.MetaPointer.Address + section.Offset, System.IO.SeekOrigin.Begin);
            var tagBlock = reader.ReadObject<TagBlock>();
            for (int i = 0; i < tagBlock.Count; i++)
            {
                reader.Seek(tagBlock.Pointer.Address + section.BlockSize * i + refOffset, System.IO.SeekOrigin.Begin);
                Bsps.Add(reader.ReadObject<TagReference>());
            }
        }

        private void ReadSkies()
        {
            var reader = ScenarioTag.CacheFile.CreateReader(ScenarioTag.CacheFile.DefaultAddressTranslator);

            var section = Sections["skies"];
            var refOffset = section.Node.SelectSingleNode("*[@id='tagreference']").GetIntAttribute("offset") ?? 0;

            reader.Seek(ScenarioTag.MetaPointer.Address + section.Offset, System.IO.SeekOrigin.Begin);
            var tagBlock = reader.ReadObject<TagBlock>();
            for (int i = 0; i < tagBlock.Count; i++)
            {
                reader.Seek(tagBlock.Pointer.Address + section.BlockSize * i + refOffset, System.IO.SeekOrigin.Begin);
                Skies.Add(reader.ReadObject<TagReference>());
            }
        }

        private void ReadObjectNames()
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

        private void ReadPalettes()
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

        private void ReadPlacements()
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
                    var placement = new ObjectPlacement(this, paletteName);
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
        #endregion

        private void DisplayItems()
        {
            SelectedItemIndex = -1;
            Items.Clear();

            if (selectedNode == null)
                return;

            var paletteKey = PaletteType.FromNodeType(SelectedNodeType);
            if (paletteKey != null)
            {
                DisplayPaletteItems(Palettes[paletteKey]);
                return;
            }

            //load other stuff like sandbox items and trigger volumes
        }

        private void DisplayPaletteItems(PaletteDefinition palette)
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
            PropertyView?.ShowProperties(SelectedNodeType, -1);
            RenderView?.SelectPalette(SelectedNodeType);
            DisplayItems();
        }

        private void OnSelectedItemChanged()
        {
            PropertyView?.ShowProperties(SelectedNodeType, SelectedItemIndex);
            RenderView?.SelectObject(SelectedNodeType, SelectedItemIndex);
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

    public class ScenarioSection
    {
        public XmlNode Node { get; set; }
        public string Name { get; set; }
        public int Offset { get; set; }
        public int BlockSize { get; set; }
    }

    public class ObjectPlacement
    {
        private readonly ScenarioModel parent;
        private readonly string paletteKey;

        public ObjectPlacement(ScenarioModel parent, string paletteKey)
        {
            this.parent = parent;
            this.paletteKey = paletteKey;
        }

        public int PaletteIndex { get; set; }
        public int NameIndex { get; set; }
        public RealVector3D Position { get; set; }
        public RealVector3D Rotation { get; set; }
        public float Scale { get; set; }
        public StringId Variant { get; set; }

        public string GetDisplayName()
        {
            var palette = parent.Palettes[paletteKey];

            if (PaletteIndex < 0 || PaletteIndex >= palette.Palette.Count)
                return "<invalid>";

            if (NameIndex >= 0)
                return parent.ObjectNames[NameIndex];
            else return palette.Palette[PaletteIndex].Tag.FileName();
        }
    }
}
