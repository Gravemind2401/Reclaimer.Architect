using Prism.Mvvm;
using Reclaimer.Plugins.MetaViewer;
using Reclaimer.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Models.Ai
{
    public abstract class AiNamedBlock : BindableBase, IMetaUpdateReceiver
    {
        internal BlockReference BlockReference { get; }
        internal int BlockIndex { get; }

        private string name;
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        public AiNamedBlock(BlockReference blockRef, int index)
        {
            BlockReference = blockRef;
            BlockIndex = index;
        }

        public virtual void UpdateFromMetaValue(MetaValueBase meta, string fieldId)
        {
            if (fieldId == FieldId.Name)
                Name = ((Plugins.MetaViewer.Halo3.StringValue)meta).Value;
        }
    }
}
