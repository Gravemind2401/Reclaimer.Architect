using Adjutant.Blam.Common;
using Prism.Mvvm;
using Reclaimer.Blam.Common;
using Reclaimer.Plugins.MetaViewer;
using Reclaimer.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Reclaimer.Models
{
    public class ObjectName : BindableBase
    {
        protected ScenarioModel Parent { get; }

        internal int BlockIndex { get; }
        internal ScenarioSection BlockReference => Parent.Sections[Section.ObjectNames];

        internal long BlockStartAddress => BlockIndex < 0 ? -1 : BlockReference.TagBlock.Pointer.Address + BlockIndex * BlockReference.BlockSize;

        public string Name { get; set; }

        private short type;
        public short Type
        {
            get { return type; }
            set { SetProperty(ref type, value, OnValueChanged); }
        }

        private int placementIndex;
        public int PlacementIndex
        {
            get { return placementIndex; }
            set { SetProperty(ref placementIndex, value, OnValueChanged); }
        }

        public ObjectName(ScenarioModel parent, int index)
        {
            Parent = parent;
            BlockIndex = index;
        }

        public bool ComparePalette(string paletteType)
        {
            if (Parent.ScenarioTag.CacheFile.CacheType < CacheType.Halo4Beta)
                return string.Equals(paletteType, ((PlacementTypeHalo3)Type).ToString(), StringComparison.OrdinalIgnoreCase);
            else
                return string.Equals(paletteType, ((PlacementTypeHalo4)Type).ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private void OnValueChanged()
        {
            if (Parent.IsBusy)
                return;

            //var nameOffset = OffsetById(BlockReference.Node, FieldId.Name);
            var typeOffset = OffsetById(BlockReference.Node, FieldId.PlacementType);
            var indexOffset = OffsetById(BlockReference.Node, FieldId.PlacementIndex);

            using (var writer = Parent.CreateWriter())
            {
                writer.Seek(BlockStartAddress + typeOffset, SeekOrigin.Begin);
                writer.Write(Type);

                writer.Seek(BlockStartAddress + indexOffset, SeekOrigin.Begin);
                writer.Write((short)PlacementIndex);
            }
        }

        private int OffsetById(XmlNode node, string fieldId)
        {
            return node.SelectSingleNode($"*[@id='{fieldId}']")?.GetIntAttribute("offset") ?? -1;
        }

        public override string ToString() => Name;
    }
}
