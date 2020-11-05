using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Controls
{
    public interface IMeshNode
    {
        string Name { get; }
        bool IsVisible { get; set; }
        SharpDX.BoundingBox GetNodeBounds();
    }
}
