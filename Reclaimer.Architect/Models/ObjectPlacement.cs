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
    public class ObjectPlacement : ScenarioObject
    {
        internal readonly string PaletteKey;

        private short paletteIndex;
        public short PaletteIndex
        {
            get { return paletteIndex; }
            set
            {
                var oldValue = paletteIndex;
                if (SetProperty(ref paletteIndex, value, FieldId.PaletteIndex))
                {
                    Parent.RenderView?.RefreshObject(PaletteKey, this, FieldId.PaletteIndex);
                    if (oldValue == -1) //reselect because theres actually something to select now
                        Parent.RenderView.SelectObject(Parent.SelectedNode, Parent.SelectedItemIndex);
                }
            }
        }

        private short nameIndex;
        public short NameIndex
        {
            get { return nameIndex; }
            set { SetProperty(ref nameIndex, value, FieldId.NameIndex); }
        }

        private RealVector3D rotation;
        public RealVector3D Rotation
        {
            get { return rotation; }
            set { SetProperty(ref rotation, value, FieldId.Rotation); }
        }

        private RealVector4D qrotation;
        public RealVector4D QRotation
        {
            get { return qrotation; }
            set { SetProperty(ref qrotation, value, FieldId.QRotation); }
        }

        private float scale;
        public float Scale
        {
            get { return scale; }
            set { SetProperty(ref scale, value, FieldId.Scale); }
        }

        private StringId variant;
        public StringId Variant
        {
            get { return variant; }
            set
            {
                if (SetProperty(ref variant, value, FieldId.Variant))
                    Parent.RenderView?.RefreshObject(PaletteKey, this, FieldId.Variant);
            }
        }

        public ObjectPlacement(ScenarioModel parent, string paletteKey)
            : base(parent)
        {
            PaletteKey = paletteKey;
        }

        protected override long GetFieldAddress(string fieldId)
        {
            var palette = Parent.Palettes[PaletteKey];
            var block = palette.PlacementBlockRef;
            var index = palette.Placements.IndexOf(this);
            var fieldOffset = palette.PlacementsNode.SelectSingleNode($"*[@id='{fieldId}']").GetIntAttribute("offset") ?? 0;

            return block.TagBlock.Pointer.Address + block.BlockSize * index + fieldOffset;
        }

        public override string GetDisplayName()
        {
            var palette = Parent.Palettes[PaletteKey];

            if (PaletteIndex < 0 || PaletteIndex >= palette.Palette.Count)
                return "<invalid>";

            if (NameIndex >= 0 && NameIndex < Parent.ObjectNames.Count)
                return Parent.ObjectNames[NameIndex];
            else return palette.Palette[PaletteIndex].Tag?.FileName() ?? "<null>";
        }

        public override void UpdateFromMetaValue(MetaValueBase meta, string fieldId)
        {
            switch (fieldId)
            {
                case FieldId.Position:
                case FieldId.Rotation:
                    var multi = meta as MultiValue;
                    var vector = new RealVector3D(multi.Value1, multi.Value2, multi.Value3);
                    if (fieldId == FieldId.Position)
                        Position = vector;
                    else Rotation = vector;
                    break;
                case FieldId.QRotation:
                    multi = meta as MultiValue;
                    QRotation = new RealVector4D(multi.Value1, multi.Value2, multi.Value3, multi.Value4);
                    break;
                case FieldId.Scale:
                    var simple = meta as SimpleValue;
                    Scale = float.Parse(simple.Value.ToString());
                    break;
                case FieldId.PaletteIndex:
                    var blockIndex = meta as BlockIndexValue;
                    PaletteIndex = short.Parse(blockIndex.Value.ToString());
                    break;
            }
        }
    }
}
