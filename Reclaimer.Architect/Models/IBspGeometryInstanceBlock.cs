using Adjutant.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Models
{
    public interface IBspGeometryInstanceBlock
    {
        float TransformScale { get; }
        Matrix4x4 Transform { get; }
        short SectionIndex { get; }
        RealVector3D BoundingSpherePosition { get; }
        float BoundingSphereRadius { get; }
    }
}
