using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Models
{
    public interface IStructureBspHandler
    {
        void SetScenario(StructureBspModel bspModel);
        void ClearScenario();
    }

    public interface IStructureBspHierarchyView : IStructureBspHandler
    {

    }

    public interface IStructureBspPropertyView : IStructureBspHandler
    {
        InstancePlacement CurrentItem { get; set; }
    }

    public interface IStructureBspRenderView : IStructureBspHandler
    {
        IEnumerable<InstancePlacement> GetPlacements();
    }
}
