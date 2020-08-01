using Adjutant.Blam.Common;
using Adjutant.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Geometry
{
    public class CompositeGeometryModel
    {
        public IGeometryModel BaseModel { get; set; }
        public List<GeometryModelVariant> Variants { get; set; }
    }

    public class GeometryModelVariant
    {
        public string Name { get; set; }
        public List<VariantRegion> Regions { get; set; }
        public List<GeometryModelAttachment> Attachments { get; set; }
    }

    public class VariantRegion
    {
        public string Name { get; set; }
        public int BaseRegionIndex { get; set; }
        public int ParentVariantIndex { get; set; }
        public List<VariantPermutation> Permutations { get; set; }
    }

    public class VariantPermutation
    {
        public string Name { get; set; }
        public int BasePermutationIndex { get; set; }
    }

    public class GeometryModelAttachment
    {
        public string ParentMarker { get; set; }
        public string ChildMarker { get; set; }
        public string ChildVariant { get; set; }
        public CompositeGeometryModel ChildModel { get; set; }
    }

    public static class CompositeModelFactory
    {
        private static readonly string[] SupportedTags = new[] { "hlmt", "weap", "vehi", "bipd", "scen" };

        public static bool IsTagSupported(IIndexItem tag) => SupportedTags.Any(s => tag.ClassCode.ToLower() == s);

        public static bool TryGetModel(IIndexItem item, out CompositeGeometryModel model)
        {
            model = null;
            return false;
        }
    }
}
