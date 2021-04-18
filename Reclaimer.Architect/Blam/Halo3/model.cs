using Adjutant.Blam.Common;
using Adjutant.Geometry;
using Adjutant.Spatial;
using Adjutant.Utilities;
using Reclaimer.Models;
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

        [Offset(100)]
        public BlockCollection<ModelVariant> Variants { get; set; }

        public ModelConfig ToModelConfig()
        {
            var config = new ModelConfig();
            config.RenderModelTag = RenderModel.Tag;

            foreach (var v in Variants)
            {
                var variant = new VariantConfig
                {
                    Name = v.Name,
                    RegionLookup = v.RuntimeModelRegions
                };

                config.Variants.Add(variant);

                foreach (var r in v.Regions)
                {
                    var region = new VariantRegionConfig
                    {
                        Name = r.Name,
                        ParentVariantIndex = r.ParentVariantIndex,
                        BaseRegionIndex = r.RuntimeRegionIndex
                    };

                    variant.Regions.Add(region);

                    foreach (var p in r.Permutations)
                    {
                        region.Permutations.Add(new VariantPermutationConfig
                        {
                            Name = p.Name,
                            BasePermutationIndex = p.RenderPermutationIndex
                        });
                    }
                }

                foreach (var att in v.Attachments)
                {
                    variant.Attachments.Add(new AttachmentConfig
                    {
                        ParentMarker = att.ParentMarker,
                        ChildMarker = att.ChildMarker,
                        ChildVariant = att.ChildVariant,
                        ChildTag = att.ChildObject.Tag
                    });
                }
            }

            return config;
        }
    }

    [FixedSize(56, MaxVersion = (int)CacheType.Halo3ODST)]
    [FixedSize(80, MinVersion = (int)CacheType.Halo3ODST)]
    public class ModelVariant
    {
        [Offset(0)]
        public StringId Name { get; set; }

        public byte[] RuntimeModelRegions { get; } = new byte[16];

        #region Runtime Region Indexes
        [Offset(4, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(28, MinVersion = (int)CacheType.Halo3ODST)]
        public byte RuntimeRegionIndex00
        {
            get { return RuntimeModelRegions[0]; }
            set { RuntimeModelRegions[0] = value; }
        }

        [Offset(5, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(29, MinVersion = (int)CacheType.Halo3ODST)]
        public byte RuntimeRegionIndex01
        {
            get { return RuntimeModelRegions[1]; }
            set { RuntimeModelRegions[1] = value; }
        }

        [Offset(6, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(30, MinVersion = (int)CacheType.Halo3ODST)]
        public byte RuntimeRegionIndex02
        {
            get { return RuntimeModelRegions[2]; }
            set { RuntimeModelRegions[2] = value; }
        }

        [Offset(7, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(31, MinVersion = (int)CacheType.Halo3ODST)]
        public byte RuntimeRegionIndex03
        {
            get { return RuntimeModelRegions[3]; }
            set { RuntimeModelRegions[3] = value; }
        }

        [Offset(8, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(32, MinVersion = (int)CacheType.Halo3ODST)]
        public byte RuntimeRegionIndex04
        {
            get { return RuntimeModelRegions[4]; }
            set { RuntimeModelRegions[4] = value; }
        }

        [Offset(9, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(33, MinVersion = (int)CacheType.Halo3ODST)]
        public byte RuntimeRegionIndex05
        {
            get { return RuntimeModelRegions[5]; }
            set { RuntimeModelRegions[5] = value; }
        }

        [Offset(10, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(34, MinVersion = (int)CacheType.Halo3ODST)]
        public byte RuntimeRegionIndex06
        {
            get { return RuntimeModelRegions[6]; }
            set { RuntimeModelRegions[6] = value; }
        }

        [Offset(11, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(35, MinVersion = (int)CacheType.Halo3ODST)]
        public byte RuntimeRegionIndex07
        {
            get { return RuntimeModelRegions[7]; }
            set { RuntimeModelRegions[7] = value; }
        }

        [Offset(12, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(36, MinVersion = (int)CacheType.Halo3ODST)]
        public byte RuntimeRegionIndex08
        {
            get { return RuntimeModelRegions[8]; }
            set { RuntimeModelRegions[8] = value; }
        }

        [Offset(13, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(37, MinVersion = (int)CacheType.Halo3ODST)]
        public byte RuntimeRegionIndex09
        {
            get { return RuntimeModelRegions[9]; }
            set { RuntimeModelRegions[9] = value; }
        }

        [Offset(14, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(38, MinVersion = (int)CacheType.Halo3ODST)]
        public byte RuntimeRegionIndex10
        {
            get { return RuntimeModelRegions[10]; }
            set { RuntimeModelRegions[10] = value; }
        }

        [Offset(15, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(39, MinVersion = (int)CacheType.Halo3ODST)]
        public byte RuntimeRegionIndex11
        {
            get { return RuntimeModelRegions[11]; }
            set { RuntimeModelRegions[11] = value; }
        }

        [Offset(16, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(40, MinVersion = (int)CacheType.Halo3ODST)]
        public byte RuntimeRegionIndex12
        {
            get { return RuntimeModelRegions[12]; }
            set { RuntimeModelRegions[12] = value; }
        }

        [Offset(17, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(41, MinVersion = (int)CacheType.Halo3ODST)]
        public byte RuntimeRegionIndex13
        {
            get { return RuntimeModelRegions[13]; }
            set { RuntimeModelRegions[13] = value; }
        }

        [Offset(18, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(42, MinVersion = (int)CacheType.Halo3ODST)]
        public byte RuntimeRegionIndex14
        {
            get { return RuntimeModelRegions[14]; }
            set { RuntimeModelRegions[14] = value; }
        }

        [Offset(19, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(43, MinVersion = (int)CacheType.Halo3ODST)]
        public byte RuntimeRegionIndex15
        {
            get { return RuntimeModelRegions[15]; }
            set { RuntimeModelRegions[15] = value; }
        } 
        #endregion

        [Offset(20, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(44, MinVersion = (int)CacheType.Halo3ODST)]
        public BlockCollection<ModelRegion> Regions { get; set; }

        [Offset(32, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(56, MinVersion = (int)CacheType.Halo3ODST)]
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
