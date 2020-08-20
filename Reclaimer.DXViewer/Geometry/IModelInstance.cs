using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Helix = HelixToolkit.Wpf.SharpDX;

namespace Reclaimer.Geometry
{
    public interface IModelInstance
    {
        string Name { get; }
        Helix.GroupModel3D Element { get; }
        void SetElementVisible(object key, bool visible);
        SharpDX.BoundingBox GetElementBounds(object key);
    }
}
