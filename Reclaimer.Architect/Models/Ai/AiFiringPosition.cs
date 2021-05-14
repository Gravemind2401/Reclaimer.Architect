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
    public class AiFiringPosition : ScenarioObject
    {
        internal AiZone Zone { get; }
        internal BlockReference BlockReference { get; }
        internal override int BlockIndex { get; }

        public AiFiringPosition(ScenarioModel parent, AiZone zone, BlockReference blockRef, int index)
            : base(parent)
        {
            Zone = zone;
            BlockReference = blockRef;
            BlockIndex = index;
        }

        protected override long GetFieldAddress(string fieldId)
        {
            var fieldOffset = Parent.SquadHierarchy.AiNodes[AiSection.FiringPositions].SelectSingleNode($"*[@id='{fieldId}']").GetIntAttribute("offset") ?? 0;
            return BlockReference.TagBlock.Pointer.Address + BlockReference.BlockSize * BlockIndex + fieldOffset;
        }

        public override string GetDisplayName()
        {
            return $"firing position {Zone.FiringPositions.IndexOf(this)}";
        }

        public override void UpdateFromMetaValue(MetaValueBase meta, string fieldId)
        {
            switch (fieldId)
            {
                case FieldId.Position:
                    var multi = meta as MultiValue;
                    Position = new RealVector3D(multi.Value1, multi.Value2, multi.Value3);
                    break;
            }
        }
    }
}