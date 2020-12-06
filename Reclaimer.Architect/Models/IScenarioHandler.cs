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
        void ShowCurrentSelection();
    }

    public interface IScenarioPropertyView : IScenarioHandler
    {
        object CurrentItem { get; }
        void ShowProperties(NodeType nodeType, int itemIndex);
        void ClearProperties();
        void SetValue(string id, object value);
    }

    public interface IScenarioRenderView : IScenarioHandler
    {
        void SelectPalette(NodeType nodeType);
        void SelectObject(NodeType nodeType, int itemIndex);
        void NavigateToObject(NodeType nodeType, int itemIndex);
        void RefreshPalette(string paletteKey, int index);
        void RefreshObject(string paletteKey, ObjectPlacement placement, string fieldId);
    }
}
