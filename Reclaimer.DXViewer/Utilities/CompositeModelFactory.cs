using Adjutant.Blam.Common;
using Adjutant.Geometry;
using Adjutant.Utilities;
using Reclaimer.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Utilities
{
    public static class CompositeModelFactory
    {
        private static readonly string[] supportedTags = new[] { "hlmt", "weap", "vehi", "bipd", "scen" };

        public static bool IsTagSupported(IIndexItem tag) => supportedTags.Any(s => tag.ClassCode.ToLower() == s);

        public static bool TryGetModel(IIndexItem item, out CompositeGeometryModel model)
        {
            string dv;
            return TryGetModel(item, out model, out dv);
        }

        public static bool TryGetModel(IIndexItem item, out CompositeGeometryModel model, out string defaultVariant)
        {
            model = null;
            defaultVariant = null;

            if (item == null)
                return false;

            var code = supportedTags.FirstOrDefault(s => s.Equals(item.ClassCode, StringComparison.OrdinalIgnoreCase));
            if (code == null)
                return false;

            item = GetModelTag(item, ref defaultVariant);
            if (item == null)
                return false;


            switch (item.CacheFile.CacheType)
            {
                case CacheType.Halo3Retail:
                    model = item.ReadMetadata<Blam.Halo3.model>().ToCompositeModel();
                    break;
            }

            return model != null;
        }

        private static IIndexItem GetModelTag(IIndexItem parent, ref string defaultVariant)
        {
            if (parent.ClassCode.Equals(supportedTags[0], StringComparison.OrdinalIgnoreCase))
                return parent;

            switch (parent.CacheFile.CacheType)
            {
                case CacheType.Halo3Retail:
                    var meta = parent.ReadMetadata<Blam.Halo3.@object>();
                    defaultVariant = meta.DefaultVariant;
                    return meta.Model.Tag;

                default: return null;
            }
        }

        public static CompositeGeometryModel ToCompositeModel(this Blam.Halo3.model hlmt)
        {
            var result = new CompositeGeometryModel();

            IRenderGeometry baseModel;
            if (!ContentFactory.TryGetGeometryContent(hlmt.RenderModel.Tag, out baseModel))
                return null;

            result.BaseModel = baseModel.ReadGeometry(0);

            foreach (var v in hlmt.Variants)
            {
                var variant = new GeometryModelVariant
                {
                    Name = v.Name,
                    RegionLookup = v.RuntimeModelRegions
                };

                result.Variants.Add(variant);

                foreach (var r in v.Regions)
                {
                    var region = new VariantRegion
                    {
                        Name = r.Name,
                        ParentVariantIndex = r.ParentVariantIndex,
                        BaseRegionIndex = r.RuntimeRegionIndex
                    };

                    variant.Regions.Add(region);

                    foreach (var p in r.Permutations)
                    {
                        region.Permutations.Add(new VariantPermutation
                        {
                            Name = p.Name,
                            BasePermutationIndex = p.RenderPermutationIndex
                        });
                    }
                }

                foreach (var att in v.Attachments)
                {
                    CompositeGeometryModel m;
                    string dv;

                    if (!TryGetModel(att.ChildObject.Tag, out m, out dv))
                        continue;

                    var attachment = new GeometryModelAttachment
                    {
                        ParentMarker = att.ParentMarker,
                        ChildMarker = att.ChildMarker,
                        ChildVariant = att.ChildVariant,
                        ChildModel = m
                    };

                    if (string.IsNullOrEmpty(att.ChildVariant))
                        attachment.ChildVariant = dv;

                    variant.Attachments.Add(attachment);
                }
            }

            return result;
        }
    }
}
