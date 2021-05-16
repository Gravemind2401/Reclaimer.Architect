using Adjutant.Blam.Common;
using Adjutant.Spatial;
using Reclaimer.Plugins.MetaViewer;
using Reclaimer.Plugins.MetaViewer.Halo3;
using Reclaimer.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Models.Ai
{
    public class AiStartingLocation : ScenarioObject
    {
        internal readonly string SectionKey;

        internal BlockReference BlockReference { get; }
        internal override int BlockIndex { get; }

        private StringId name;
        public StringId Name
        {
            get { return name; }
            set { SetProperty(ref name, value, FieldId.Name); }
        }

        private RealVector3D rotation;
        public RealVector3D Rotation
        {
            get { return rotation; }
            set { SetProperty(ref rotation, value, FieldId.Rotation); }
        }

        public AiStartingLocation(ScenarioModel parent, BlockReference blockRef, int index, string sectionKey)
            : base(parent)
        {
            SectionKey = sectionKey;
            BlockReference = blockRef;
            BlockIndex = index;
        }

        protected override long GetFieldAddress(string fieldId)
        {
            var fieldOffset = Parent.SquadHierarchy.AiNodes[SectionKey].SelectSingleNode($"*[@id='{fieldId}']").GetIntAttribute("offset") ?? 0;
            return BlockReference.TagBlock.Pointer.Address + BlockReference.BlockSize * BlockIndex + fieldOffset;
        }

        public override string GetDisplayName()
        {
            return Name.Value;
        }

        public override void UpdateFromMetaValue(MetaValueBase meta, string fieldId)
        {
            switch (fieldId)
            {
                case FieldId.Name:
                    var stringId = meta as StringIdValue;
                    var cache = Parent.ScenarioTag.CacheFile;
                    var intValue = cache.StringIndex.GetStringId(stringId.Value);
                    if (intValue >= 0)
                        Name = new StringId(intValue, cache);
                    break;
                case FieldId.Position:
                case FieldId.Rotation:
                    var multi = meta as MultiValue;
                    var vector = new RealVector3D(multi.Value1, multi.Value2, multi.Value3);
                    if (fieldId == FieldId.Position)
                        Position = vector;
                    else Rotation = vector;
                    break;
            }
        }
    }
}