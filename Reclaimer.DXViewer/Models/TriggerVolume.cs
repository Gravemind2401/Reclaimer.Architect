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
    public class TriggerVolume : BindableBase
    {
        public static readonly SharpDX.Color DefaultColour = new SharpDX.Color(0, 1, 0, 0.5f);
        public static readonly SharpDX.Color SelectedColour = new SharpDX.Color(0, 0.5f, 1, 0.5f);

        private readonly ScenarioModel parent;

        private StringId name;
        public StringId Name
        {
            get { return name; }
            set { SetProperty(ref name, value, FieldId.Name); }
        }

        private RealVector3D position;
        public RealVector3D Position
        {
            get { return position; }
            set { SetProperty(ref position, value, FieldId.Position); }
        }

        private RealVector3D size;
        public RealVector3D Size
        {
            get { return size; }
            set { SetProperty(ref size, value, FieldId.Size); }
        }

        public TriggerVolume(ScenarioModel parent)
        {
            this.parent = parent;
        }

        private bool SetProperty<T>(ref T storage, T value, string fieldId, [CallerMemberName] string propertyName = null)
        {
            if (parent.IsBusy)
                return base.SetProperty(ref storage, value, propertyName);

            if (!base.SetProperty(ref storage, value, propertyName))
                return false;

            var section = parent.Sections["triggervolumes"];
            var index = parent.TriggerVolumes.IndexOf(this);
            var fieldOffset = section.Node.SelectSingleNode($"*[@id='{fieldId}']").GetIntAttribute("offset") ?? 0;

            using (var writer = parent.CreateWriter())
            {
                writer.Seek(section.TagBlock.Pointer.Address + section.BlockSize * index + fieldOffset, SeekOrigin.Begin);

                if (typeof(T) == typeof(StringId))
                    writer.Write(((StringId)(object)value).Id);
                else writer.WriteObject(value);
            }

            if (parent.PropertyView?.CurrentItem == this)
                parent.PropertyView.SetValue(fieldId, value);

            return true;
        }

        public override string ToString() => Name;
    }
}