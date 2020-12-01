using Reclaimer.Plugins.MetaViewer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Controls
{
    public interface IMetaViewerHost
    {
        bool ShowInvisibles { get; }
        ObservableCollection<MetaValueBase> Metadata { get; }
    }
}
