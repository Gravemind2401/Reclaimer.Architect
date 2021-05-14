using Adjutant.Blam.Common;
using Adjutant.Spatial;
using Reclaimer.Plugins.MetaViewer;
using Reclaimer.Plugins.MetaViewer.Halo3;
using Reclaimer.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Models
{
    public class TriggerVolume : ScenarioObject
    {
        public static readonly SharpDX.Color DefaultColour = new SharpDX.Color(0, 1, 0, 0.5f);
        public static readonly SharpDX.Color SelectedColour = new SharpDX.Color(0, 0.5f, 1, 0.5f);

        internal override int BlockIndex => Parent.TriggerVolumes.IndexOf(this);

        private StringId name;
        public StringId Name
        {
            get { return name; }
            set { SetProperty(ref name, value, FieldId.Name); }
        }

        private RealVector3D forwardVector;
        public RealVector3D ForwardVector
        {
            get { return forwardVector; }
            set { SetProperty(ref forwardVector, value, FieldId.ForwardVector); }
        }

        private RealVector3D upVector;
        public RealVector3D UpVector
        {
            get { return upVector; }
            set { SetProperty(ref upVector, value, FieldId.UpVector); }
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
            var section = Parent.Sections[Section.TriggerVolumes];
            var fieldOffset = section.Node.SelectSingleNode($"*[@id='{fieldId}']").GetIntAttribute("offset") ?? 0;

            return section.TagBlock.Pointer.Address + section.BlockSize * BlockIndex + fieldOffset;
        }

        public override string GetDisplayName() => $"[{BlockIndex:D3}] " + (string.IsNullOrEmpty(Name) ? "<none>" : Name);

        public override void UpdateFromMetaValue(MetaValueBase meta, string fieldId)
        {
            var multi = meta as MultiValue;
            var vector = new RealVector3D(multi.Value1, multi.Value2, multi.Value3);

            switch (fieldId)
            {
                case FieldId.ForwardVector:
                    ForwardVector = vector;
                    break;
                case FieldId.UpVector:
                    UpVector = vector;
                    break;
                case FieldId.Position:
                    Position = vector;
                    break;
                case FieldId.Size:
                    Size = vector;
                    break;
            }
        }

        public void CopyFrom(TriggerVolume other)
        {
            Name = other.Name;
            Position = other.Position;
            ForwardVector = other.ForwardVector;
            UpVector = other.UpVector;
            Size = other.Size;
        }
    }
}