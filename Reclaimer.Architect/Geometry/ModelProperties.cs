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

        public int MeshCount { get; }
        public IReadOnlyList<int> StandardMeshes { get; }
        public IReadOnlyList<int> InstanceMeshes { get; }

        public ModelProperties(IGeometryModel source)
        {
            Name = source.Name;
            Regions = source.Regions.ToList();
            MarkerGroups = source.MarkerGroups.ToList();
            Nodes = source.Nodes.ToList();
            Materials = source.Materials.ToList();

            var standard = new List<int>();
            var instances = new List<int>();

            for (int i = 0; i < source.Meshes.Count; i++)
            {
                if (source.Meshes[i].IsInstancing)
                    instances.Add(i);
                else standard.Add(i);
            }

            MeshCount = source.Meshes.Count;
            StandardMeshes = standard;
            InstanceMeshes = instances;
        }
    }
}
