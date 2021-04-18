using Adjutant.Blam.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Models
{
    public class ModelConfig
    {
        public static readonly ModelConfig Empty = new ModelConfig();

        public IIndexItem ModelTag { get; set; }
        public IIndexItem RenderModelTag { get; set; }
        public List<VariantConfig> Variants { get; set; } = new List<VariantConfig>();

        public override string ToString() => ModelTag?.ToString();

        public static ModelConfig FromIndexItem(IIndexItem item)
        {
            ModelConfig result;
            switch (item.CacheFile.CacheType)
            {
                case CacheType.Halo3Retail:
                case CacheType.MccHalo3:
                case CacheType.MccHalo3U4:
                case CacheType.Halo3ODST:
                case CacheType.MccHalo3ODST:
                    result = item.ReadMetadata<Blam.Halo3.model>().ToModelConfig();
                    break;

                case CacheType.HaloReachRetail:
                case CacheType.MccHaloReach:
                case CacheType.MccHaloReachU3:
                    result = item.ReadMetadata<Blam.HaloReach.model>().ToModelConfig();
                    break;

                case CacheType.Halo4Retail:
                case CacheType.MccHalo4:
                    result = item.ReadMetadata<Blam.Halo4.model>().ToModelConfig();
                    break;

                default: return null;
            }

            result.ModelTag = item;
            return result;
        }
    }

    public class VariantConfig
    {
        public string Name { get; set; }
        public byte[] RegionLookup { get; set; }
        public List<VariantRegionConfig> Regions { get; set; } = new List<VariantRegionConfig>();
        public List<AttachmentConfig> Attachments { get; set; } = new List<AttachmentConfig>();

        public override string ToString() => Name;
    }

    public class VariantRegionConfig
    {
        public string Name { get; set; }
        public int BaseRegionIndex { get; set; }
        public int ParentVariantIndex { get; set; }
        public List<VariantPermutationConfig> Permutations { get; set; } = new List<VariantPermutationConfig>();

        public override string ToString() => Name;
    }

    public class VariantPermutationConfig
    {
        public string Name { get; set; }
        public int BasePermutationIndex { get; set; }

        public override string ToString() => Name;
    }

    public class AttachmentConfig
    {
        public string ParentMarker { get; set; }
        public string ChildMarker { get; set; }
        public string ChildVariant { get; set; }
        public IIndexItem ChildTag { get; set; }

        public override string ToString() => ChildTag?.ToString();
    }
}
