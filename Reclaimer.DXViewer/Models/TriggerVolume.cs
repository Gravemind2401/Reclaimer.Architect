using Adjutant.Blam.Common;
using Adjutant.Spatial;
using Prism.Mvvm;
using Reclaimer.Plugins.MetaViewer;
using Reclaimer.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Models
{
    public class TriggerVolume : ScenarioObject
    {
        public static readonly SharpDX.Color DefaultColour = new SharpDX.Color(0, 1, 0, 0.5f);
        public static readonly SharpDX.Color SelectedColour = new SharpDX.Color(0, 0.5f, 1, 0.5f);

        private StringId name;
        public StringId Name
        {
            get { return name; }
            set { SetProperty(ref name, value, FieldId.Name); }
        }

        private RealVector3D size;
        public RealVector3D Size
        {
            get { return size; }
            set { SetProperty(ref size, value, FieldId.Size); }
        }

        public TriggerVolume(ScenarioModel parent)
            : base(parent)
        { }

        protected override long GetFieldAddress(string fieldId)
        {
            var section = Parent.Sections["triggervolumes"];
            var index = Parent.TriggerVolumes.IndexOf(this);
            var fieldOffset = section.Node.SelectSingleNode($"*[@id='{fieldId}']").GetIntAttribute("offset") ?? 0;

            return section.TagBlock.Pointer.Address + section.BlockSize * index + fieldOffset;
        }

        public override string GetDisplayName() => Name;
    }
}