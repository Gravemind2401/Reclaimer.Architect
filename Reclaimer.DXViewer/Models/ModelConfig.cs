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
            switch (item.CacheFile.CacheType)
            {
                case CacheType.Halo3Retail:
                    var meta = item.ReadMetadata<Blam.Halo3.model>();
                    return FromMetadata(item, meta);

                default: return null;
            }
        }

        private static ModelConfig FromMetadata(IIndexItem item, Blam.Halo3.model hlmt)
        {
            var config = new ModelConfig
            {
                ModelTag = item,
                RenderModelTag = hlmt.RenderModel.Tag
            };

            foreach (var v in hlmt.Variants)
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
