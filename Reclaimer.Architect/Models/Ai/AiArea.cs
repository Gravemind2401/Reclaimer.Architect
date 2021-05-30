using Adjutant.Spatial;
using Reclaimer.Plugins.MetaViewer;
using Reclaimer.Plugins.MetaViewer.Halo3;
using Reclaimer.Resources;
using Reclaimer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Models.Ai
{
    public class AiArea : ScenarioObject
    {
        internal AiZone Zone { get; }
        internal BlockReference BlockReference { get; }
        internal override int BlockIndex { get; }

        private string name;
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value, FieldId.Name); }
        }

        public AiArea(ScenarioModel parent, AiZone zone, BlockReference blockRef, int index)
            : base(parent)
        {
            Zone = zone;
            BlockReference = blockRef;
            BlockIndex = index;
        }

        protected override long GetFieldAddress(string fieldId)
        {
            var fieldOffset = Parent.SquadHierarchy.AiNodes[AiSection.Areas].SelectSingleNode($"*[@id='{fieldId}']").GetIntAttribute("offset") ?? 0;
            return BlockReference.TagBlock.Pointer.Address + BlockReference.BlockSize * BlockIndex + fieldOffset;
        }

        public override string GetDisplayName()
        {
            return Name.AsDisplayName();
        }

        public override void UpdateFromMetaValue(MetaValueBase meta, string fieldId)
        {
            switch (fieldId)
            {
                case FieldId.Name:
                    var str = meta as StringValue;
                    Name = str.Value;
                    break;
                case FieldId.Position:
                    var multi = meta as MultiValue;
                    Position = new RealVector3D(multi.Value1, multi.Value2, multi.Value3);
                    break;
            }
        }
    }
}