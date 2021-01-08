using Reclaimer.Plugins.MetaViewer;
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
        private string name;
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value, FieldId.Name); }
        }

        public AiStartingLocation(ScenarioModel parent)
            : base(parent)
        {
        }

        public override string GetDisplayName()
        {
            return Name;
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