using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reclaimer.Plugins.MetaViewer;
using Reclaimer.Resources;
using Reclaimer.Plugins.MetaViewer.Halo3;

namespace Reclaimer.Models
{
    public class ScenarioListItem : BindableBase, IMetaUpdateReceiver
    {
        private string name;
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        public void UpdateFromMetaValue(MetaValueBase meta, string fieldId)
        {
            if (fieldId == FieldId.Name)
                Name = (meta as SimpleValue)?.Value?.ToString();
        }
    }
}
