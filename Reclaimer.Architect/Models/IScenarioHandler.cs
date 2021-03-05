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
        void ShowProperties(SceneNodeModel node, int itemIndex);
        void ClearProperties();
        void SetValue(string id, object value);
        void Reload();
    }

    public interface IScenarioRenderView : IScenarioHandler
    {
        void SelectPalette(SceneNodeModel node);
        void SelectObject(SceneNodeModel node, int itemIndex);
        void NavigateToObject(SceneNodeModel node, int itemIndex);
        void RefreshPalette(string paletteKey, int index);
        void RefreshObject(string paletteKey, ObjectPlacement placement, string fieldId);
    }
}
