﻿using Reclaimer.Plugins.MetaViewer;
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
        internal int BlockIndex { get; }

        public AiFiringPosition(ScenarioModel parent, AiZone zone, BlockReference blockRef, int index)
            : base(parent)
        {
            Zone = zone;
            BlockReference = blockRef;
            BlockIndex = index;
        }

        public override string GetDisplayName()
        {
            return $"firing position {Zone.FiringPositions.IndexOf(this)}";
        }

        public override void UpdateFromMetaValue(MetaValueBase meta, string fieldId)
        {
            throw new NotImplementedException();
        }

        protected override long GetFieldAddress(string fieldId)
        {
            throw new NotImplementedException();
        }
    }
}