using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Controls
{
    public interface IManipulatable
    {
        ManipulationFlags ManipulationFlags { get; }
    }
}
