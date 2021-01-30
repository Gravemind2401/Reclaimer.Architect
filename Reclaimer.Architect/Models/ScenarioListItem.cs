using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reclaimer.Plugins.MetaViewer;
using Reclaimer.Resources;
using Reclaimer.Plugins.MetaViewer.Halo3;
using System.Windows.Controls;

namespace Reclaimer.Models
{
    public class ScenarioListItem : ListBoxItem, IMetaUpdateReceiver
    {
        public ScenarioListItem(string content)
            : this(content, null)
        { }

        public ScenarioListItem(string content, object tag)
        {
            Content = content;
            Tag = tag;
        }

        void IMetaUpdateReceiver.UpdateFromMetaValue(MetaValueBase meta, string fieldId)
        {
            var receiver = Tag as IMetaUpdateReceiver;
            if (receiver == null)
                return;

            receiver.UpdateFromMetaValue(meta, fieldId);

            if (fieldId == FieldId.Name || fieldId == FieldId.TagReference)
                Content = receiver.GetDisplayName();
        }

        string IMetaUpdateReceiver.GetDisplayName() => (Tag as IMetaUpdateReceiver)?.GetDisplayName();
    }
}
