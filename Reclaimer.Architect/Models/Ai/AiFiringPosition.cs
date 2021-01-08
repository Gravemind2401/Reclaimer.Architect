using Reclaimer.Plugins.MetaViewer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Models.Ai
{
    public class AiFiringPosition : ScenarioObject
    {
        public AiFiringPosition(ScenarioModel parent)
            : base(parent)
        {
        }

        public override string GetDisplayName()
        {
            throw new NotImplementedException();
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