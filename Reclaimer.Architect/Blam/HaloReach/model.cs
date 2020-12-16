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

namespace Reclaimer.Blam.HaloReach
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

        [Offset(132)]
        public BlockCollection<ModelVariant> Variants { get; set; }
    }

    [FixedSize(56)]
    public class ModelVariant
    {
        [Offset(0)]
        public StringId Name { get; set; }

        public byte[] RuntimeModelRegions { get; } = new byte[16];

        #region Runtime Region Indexes
        [Offset(4)]
        public byte RuntimeRegionIndex00
        {
            get { return RuntimeModelRegions[0]; }
            set { RuntimeModelRegions[0] = value; }
        }

        [Offset(5)]
        public byte RuntimeRegionIndex01
        {
            get { return RuntimeModelRegions[1]; }
            set { RuntimeModelRegions[1] = value; }
        }

        [Offset(6)]
        public byte RuntimeRegionIndex02
        {
            get { return RuntimeModelRegions[2]; }
            set { RuntimeModelRegions[2] = value; }
        }

        [Offset(7)]
        public byte RuntimeRegionIndex03
        {
            get { return RuntimeModelRegions[3]; }
            set { RuntimeModelRegions[3] = value; }
        }

        [Offset(8)]
        public byte RuntimeRegionIndex04
        {
            get { return RuntimeModelRegions[4]; }
            set { RuntimeModelRegions[4] = value; }
        }

        [Offset(9)]
        public byte RuntimeRegionIndex05
        {
            get { return RuntimeModelRegions[5]; }
            set { RuntimeModelRegions[5] = value; }
        }

        [Offset(10)]
        public byte RuntimeRegionIndex06
        {
            get { return RuntimeModelRegions[6]; }
            set { RuntimeModelRegions[6] = value; }
        }

        [Offset(11)]
        public byte RuntimeRegionIndex07
        {
            get { return RuntimeModelRegions[7]; }
            set { RuntimeModelRegions[7] = value; }
        }

        [Offset(12)]
        public byte RuntimeRegionIndex08
        {
            get { return RuntimeModelRegions[8]; }
            set { RuntimeModelRegions[8] = value; }
        }

        [Offset(13)]
        public byte RuntimeRegionIndex09
        {
            get { return RuntimeModelRegions[9]; }
            set { RuntimeModelRegions[9] = value; }
        }

        [Offset(14)]
        public byte RuntimeRegionIndex10
        {
            get { return RuntimeModelRegions[10]; }
            set { RuntimeModelRegions[10] = value; }
        }

        [Offset(15)]
        public byte RuntimeRegionIndex11
        {
            get { return RuntimeModelRegions[11]; }
            set { RuntimeModelRegions[11] = value; }
        }

        [Offset(16)]
        public byte RuntimeRegionIndex12
        {
            get { return RuntimeModelRegions[12]; }
            set { RuntimeModelRegions[12] = value; }
        }

        [Offset(17)]
        public byte RuntimeRegionIndex13
        {
            get { return RuntimeModelRegions[13]; }
            set { RuntimeModelRegions[13] = value; }
        }

        [Offset(18)]
        public byte RuntimeRegionIndex14
        {
            get { return RuntimeModelRegions[14]; }
            set { RuntimeModelRegions[14] = value; }
        }

        [Offset(19)]
        public byte RuntimeRegionIndex15
        {
            get { return RuntimeModelRegions[15]; }
            set { RuntimeModelRegions[15] = value; }
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

    [FixedSize(12)]
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

    [FixedSize(32)]
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
