using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Models
{
    public interface IScenarioHandler
    {
        void SetScenario(ScenarioModel scenario);
        void ClearScenario();
    }

    public interface IScenarioHierarchyView : IScenarioHandler
    {

    }

    public interface IScenarioPropertyView : IScenarioHandler
    {
        void ShowProperties(NodeType nodeType, int itemIndex);
        void ClearProperties();
    }

    public interface IScenarioRenderView : IScenarioHandler
    {
        void SelectPalette(NodeType nodeType);
        void SelectObject(NodeType nodeType, int itemIndex);
        void NavigateToObject(NodeType nodeType, int itemIndex);
    }
}
