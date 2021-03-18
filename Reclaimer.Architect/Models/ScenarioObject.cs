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
    public abstract class ScenarioObject : BindableBase, IMetaUpdateReceiver
    {
        private volatile bool isBusy;

        protected ScenarioModel Parent { get; }

        private RealVector3D position;
        public RealVector3D Position
        {
            get { return position; }
            set { SetProperty(ref position, value, FieldId.Position); }
        }

        public ScenarioObject(ScenarioModel parent)
        {
            Parent = parent;
        }

        protected bool SetProperty<T>(ref T storage, T value, string fieldId, [CallerMemberName] string propertyName = null)
        {
            if (Parent.IsBusy) //if the scenario is still loading just set the value and return
                return base.SetProperty(ref storage, value, propertyName);

            //if this object is busy dont set the value to avoid cyclic property setting
            if (isBusy || !base.SetProperty(ref storage, value, propertyName))
                return false;

            var fieldAddress = GetFieldAddress(fieldId);
            if (fieldAddress < 0)
                return false;

            isBusy = true;

            using (var writer = Parent.CreateWriter())
            {
                writer.Seek(fieldAddress, SeekOrigin.Begin);

                if (typeof(T) == typeof(StringId))
                    writer.Write(((StringId)(object)value).Id);
                else writer.WriteObject(value);
            }

            if (Parent.PropertyView?.CurrentItem == this)
                Parent.PropertyView.SetValue(fieldId, value);

            isBusy = false;

            return true;
        }

        protected abstract long GetFieldAddress(string fieldId);

        public abstract string GetDisplayName();

        public abstract void UpdateFromMetaValue(MetaValueBase meta, string fieldId);

        public override string ToString() => GetDisplayName();
    }
}
