using Adjutant.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Geometry
{
    public class CompositeGeometryModel : IDisposable
    {
        public string Name { get; set; }
        public IGeometryModel BaseModel { get; set; }
        public List<GeometryModelVariant> Variants { get; set; } = new List<GeometryModelVariant>();

        public override string ToString() => Name;

        public void Dispose()
        {
            BaseModel.Dispose();
            foreach (var att in Variants.SelectMany(v => v.Attachments))
                att.ChildModel.Dispose();
        }
    }

    public class GeometryModelVariant
    {
        public string Name { get; set; }
        public byte[] RegionLookup { get; set; }
        public List<VariantRegion> Regions { get; set; } = new List<VariantRegion>();
        public List<GeometryModelAttachment> Attachments { get; set; } = new List<GeometryModelAttachment>();

        public override string ToString() => Name;
    }

    public class VariantRegion
    {
        public string Name { get; set; }
        public int BaseRegionIndex { get; set; }
        public int ParentVariantIndex { get; set; }
        public List<VariantPermutation> Permutations { get; set; } = new List<VariantPermutation>();

        public override string ToString() => Name;
    }

    public class VariantPermutation
    {
        public string Name { get; set; }
        public int BasePermutationIndex { get; set; }

        public override string ToString() => Name;
    }

    public class GeometryModelAttachment
    {
        public string ParentMarker { get; set; }
        public string ChildMarker { get; set; }
        public string ChildVariant { get; set; }
        public CompositeGeometryModel ChildModel { get; set; }

        public override string ToString() => ChildModel.BaseModel.Name;
    }
}
