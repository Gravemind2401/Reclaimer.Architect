using Adjutant.Blam.Common;
using Adjutant.Geometry;
using Adjutant.Spatial;
using Adjutant.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Endian;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Blam.Halo3
{
    public class model
    {
        [Offset(0)]
        public TagReference RenderModel { get; set; }

        [Offset(16)]
        public TagReference CollisionModel { get; set; }

        [Offset(32)]
        public TagReference Animation { get; set; }

        [Offset(48)]
        public TagReference PhysicsModel { get; set; }

        [Offset(0x64)]
        public BlockCollection<ModelVariant> Variants { get; set; }
    }

    [FixedSize(56)]
    public class ModelVariant
    {
        [Offset(0)]
        public StringId Name { get; set; }

        public byte[] RuntimeRegionIndexes { get; } = new byte[16];

        #region Runtime Region Indexes
        [Offset(4)]
        public byte RuntimeRegionIndex00
        {
            get { return RuntimeRegionIndexes[0]; }
            set { RuntimeRegionIndexes[0] = value; }
        }

        [Offset(5)]
        public byte RuntimeRegionIndex01
        {
            get { return RuntimeRegionIndexes[1]; }
            set { RuntimeRegionIndexes[1] = value; }
        }

        [Offset(6)]
        public byte RuntimeRegionIndex02
        {
            get { return RuntimeRegionIndexes[2]; }
            set { RuntimeRegionIndexes[2] = value; }
        }

        [Offset(7)]
        public byte RuntimeRegionIndex03
        {
            get { return RuntimeRegionIndexes[3]; }
            set { RuntimeRegionIndexes[3] = value; }
        }

        [Offset(8)]
        public byte RuntimeRegionIndex04
        {
            get { return RuntimeRegionIndexes[4]; }
            set { RuntimeRegionIndexes[4] = value; }
        }

        [Offset(9)]
        public byte RuntimeRegionIndex05
        {
            get { return RuntimeRegionIndexes[5]; }
            set { RuntimeRegionIndexes[5] = value; }
        }

        [Offset(10)]
        public byte RuntimeRegionIndex06
        {
            get { return RuntimeRegionIndexes[6]; }
            set { RuntimeRegionIndexes[6] = value; }
        }

        [Offset(11)]
        public byte RuntimeRegionIndex07
        {
            get { return RuntimeRegionIndexes[7]; }
            set { RuntimeRegionIndexes[7] = value; }
        }

        [Offset(12)]
        public byte RuntimeRegionIndex08
        {
            get { return RuntimeRegionIndexes[8]; }
            set { RuntimeRegionIndexes[8] = value; }
        }

        [Offset(13)]
        public byte RuntimeRegionIndex09
        {
            get { return RuntimeRegionIndexes[9]; }
            set { RuntimeRegionIndexes[9] = value; }
        }

        [Offset(14)]
        public byte RuntimeRegionIndex10
        {
            get { return RuntimeRegionIndexes[10]; }
            set { RuntimeRegionIndexes[10] = value; }
        }

        [Offset(15)]
        public byte RuntimeRegionIndex11
        {
            get { return RuntimeRegionIndexes[11]; }
            set { RuntimeRegionIndexes[11] = value; }
        }

        [Offset(16)]
        public byte RuntimeRegionIndex12
        {
            get { return RuntimeRegionIndexes[12]; }
            set { RuntimeRegionIndexes[12] = value; }
        }

        [Offset(17)]
        public byte RuntimeRegionIndex13
        {
            get { return RuntimeRegionIndexes[13]; }
            set { RuntimeRegionIndexes[13] = value; }
        }

        [Offset(18)]
        public byte RuntimeRegionIndex14
        {
            get { return RuntimeRegionIndexes[14]; }
            set { RuntimeRegionIndexes[14] = value; }
        }

        [Offset(19)]
        public byte RuntimeRegionIndex15
        {
            get { return RuntimeRegionIndexes[15]; }
            set { RuntimeRegionIndexes[15] = value; }
        } 
        #endregion

        [Offset(20)]
        public BlockCollection<ModelRegion> Regions { get; set; }

        [Offset(32)]
        public BlockCollection<ModelAttachment> Attachments { get; set; }

        public override string ToString() => Name;
    }

    [FixedSize(24)]
    public class ModelRegion
    {
        [Offset(0)]
        public StringId Name { get; set; }

        [Offset(4)]
        public byte RuntimeRegionIndex { get; set; }

        [Offset(6)]
        public short ParentVariantIndex { get; set; }

        [Offset(8)]
        public BlockCollection<ModelPermutation> Permutations { get; set; }

        public override string ToString() => Name;
    }

    [FixedSize(36)]
    public class ModelPermutation
    {
        [Offset(0)]
        public StringId Name { get; set; }

        [Offset(4)]
        public byte RenderPermutationIndex { get; set; }

        [Offset(12)]
        public BlockCollection<ModelPermutationState> States { get; set; }

        public override string ToString() => Name;
    }

    [FixedSize(32)]
    public class ModelPermutationState
    {
        [Offset(0)]
        public StringId Name { get; set; }

        [Offset(4)]
        public byte RenderPermutationIndex { get; set; }

        [Offset(6)]
        public short State { get; set; }

        public override string ToString() => Name;
    }

    [FixedSize(28)]
    public class ModelAttachment
    {
        [Offset(0)]
        public StringId ParentMarker { get; set; }

        [Offset(4)]
        public StringId ChildMarker { get; set; }

        [Offset(8)]
        public StringId ChildVariant { get; set; }

        [Offset(12)]
        public TagReference ChildObject { get; set; }

        public override string ToString() => ChildObject.ToString();
    }
}
