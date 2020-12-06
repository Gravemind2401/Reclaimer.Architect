using Adjutant.Blam.Common;
using Adjutant.Geometry;
using Adjutant.Spatial;
using Adjutant.Utilities;
using Reclaimer.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Endian;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Blam.Halo3
{
    public class scenario_structure_bsp
    {
        private readonly ICacheFile cache;
        private readonly IIndexItem item;

        public scenario_structure_bsp(ICacheFile cache, IIndexItem item)
        {
            this.cache = cache;
            this.item = item;
        }

        [Offset(432, MaxVersion = (int)CacheType.Halo3ODST)]
        [Offset(436, MinVersion = (int)CacheType.Halo3ODST)]
        public BlockCollection<BspGeometryInstanceBlock> GeometryInstances { get; set; }

    }

    [FixedSize(120)]
    public class BspGeometryInstanceBlock : IBspGeometryInstanceBlock
    {
        [Offset(0)]
        public float TransformScale { get; set; }

        [Offset(4)]
        public Matrix4x4 Transform { get; set; }

        [Offset(52)]
        public short SectionIndex { get; set; }

        [Offset(64)]
        public RealVector3D BoundingSpherePosition { get; set; }

        [Offset(76)]
        public float BoundingSphereRadius { get; set; }

        [Offset(84)]
        public StringId Name { get; set; }

        public override string ToString() => Name;
    }
}
