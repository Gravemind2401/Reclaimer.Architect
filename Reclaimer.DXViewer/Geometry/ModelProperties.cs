using Adjutant.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Geometry
{
    public class ModelProperties
    {
        public string Name { get; }
        public IReadOnlyList<IGeometryRegion> Regions { get; }
        public IReadOnlyList<IGeometryMarkerGroup> MarkerGroups { get; }
        public IReadOnlyList<IGeometryNode> Nodes { get; }
        public IReadOnlyList<IGeometryMaterial> Materials { get; }

        public ModelProperties(IGeometryModel source)
        {
            Name = source.Name;
            Regions = source.Regions.ToList();
            MarkerGroups = source.MarkerGroups.ToList();
            Nodes = source.Nodes.ToList();
            Materials = source.Materials.ToList();
        }
    }
}
