﻿using Adjutant.Spatial;
using Reclaimer.Plugins.MetaViewer;
using Reclaimer.Plugins.MetaViewer.Halo3;
using Reclaimer.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Models
{
    public class StartPosition : ScenarioObject
    {
        private RealVector2D orientation;
        public RealVector2D Orientation
        {
            get { return orientation; }
            set { SetProperty(ref orientation, value, FieldId.Orientation); }
        }

        public StartPosition(ScenarioModel parent)
            : base(parent)
        {

        }

        protected override long GetFieldAddress(string fieldId)
        {
            var section = Parent.Sections["startpositions"];
            var index = Parent.StartingPositions.IndexOf(this);
            var fieldOffset = section.Node.SelectSingleNode($"*[@id='{fieldId}']").GetIntAttribute("offset") ?? 0;

            return section.TagBlock.Pointer.Address + section.BlockSize * index + fieldOffset;
        }

        public override string GetDisplayName()
        {
            return $"starting position {Parent.StartingPositions.IndexOf(this)}";
        }

        public override void UpdateFromMetaValue(MetaValueBase meta, string fieldId)
        {
            switch (fieldId)
            {
                case "position":
                case "orientation":
                    var multi = meta as MultiValue;
                    if (fieldId == "position")
                        Position = new RealVector3D(multi.Value1, multi.Value2, multi.Value3);
                    else Orientation = new RealVector2D(multi.Value1, multi.Value2);
                    break;
            }
        }
    }
}