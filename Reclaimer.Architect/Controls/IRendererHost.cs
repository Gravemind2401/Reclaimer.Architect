using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Helix = HelixToolkit.Wpf.SharpDX;

namespace Reclaimer.Controls
{
    public interface IRendererHost
    {
        void OnElementSelected(Helix.Element3D element);
    }
}
