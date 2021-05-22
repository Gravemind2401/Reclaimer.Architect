using Reclaimer.Plugins.MetaViewer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Models
{
    internal interface IMetaUpdateReceiver : IDisplayName
    {
        void UpdateFromMetaValue(MetaValueBase meta, string fieldId);
    }
}
