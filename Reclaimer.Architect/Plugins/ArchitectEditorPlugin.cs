using Adjutant.Blam.Common;
using Adjutant.Utilities;
using Reclaimer.Models;
using Reclaimer.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Plugins
{
    public class ArchitectEditorPlugin : Plugin
    {
        public override string Name => "Architect Editor";

        public override bool CanOpenFile(OpenFileArgs args)
        {
#if DEBUG
            return args.File.OfType<IIndexItem>().Any(i => Controls.DXEditor.CanOpenTag(i))
                || args.File.OfType<IIndexItem>().Any(i => Controls.BspEditor.CanOpenTag(i));
#else
            return args.File.OfType<IIndexItem>().Any(i => Controls.DXEditor.CanOpenTag(i));
#endif
        }

        public override void OpenFile(OpenFileArgs args)
        {
            var modelTag = args.File.OfType<IIndexItem>().FirstOrDefault();
            DisplayModel(args.TargetWindow, modelTag, args.FileName);
        }

        private IEnumerable<TabOwnerModelBase> EnumerateChildren(TabOwnerModelBase obj)
        {
            var container = obj as DockContainerModel;
            if (container != null)
                return EnumerateChildren(container.Content);

            var panel = obj as SplitPanelModel;
            if (panel != null)
                return panel.Items.Union(panel.Items.SelectMany(i => EnumerateChildren(i)));

            return Enumerable.Repeat(obj, 1);
        }

        [SharedFunction]
        public void DisplayModel(ITabContentHost targetWindow, IIndexItem modelTag, string fileName)
        {
            var container = targetWindow.DocumentPanel;

            LogOutput($"Loading tag: {fileName}");

            try
            {
                if (Controls.DXEditor.CanOpenTag(modelTag))
                {
                    var hierarchyView = new Controls.HierarchyView();
                    var propertyView = new Controls.PropertyView();
                    var renderView = new Controls.DXEditor();

                    var model = new ScenarioModel(modelTag)
                    {
                        HierarchyView = hierarchyView,
                        PropertyView = propertyView,
                        RenderView = renderView,
                        LogOutput = LogOutput,
                        LogError = LogError
                    };

                    var existingToolWells = EnumerateChildren(targetWindow.DockContainer)
                        .OfType<ToolWellModel>()
                        .ToList();

                    targetWindow.DocumentPanel.AddItem(renderView.TabModel);
                    targetWindow.DockContainer.AddTool(hierarchyView.TabModel, null, System.Windows.Controls.Dock.Right);
                    targetWindow.DockContainer.AddTool(propertyView.TabModel, hierarchyView.TabModel.Parent, System.Windows.Controls.Dock.Bottom);

                    //the panels attempt to size to the contents but since the controls havent loaded yet
                    //they will have no size - we need to set a size for them.
                    hierarchyView.TabModel.Parent.PanelSize = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
                    propertyView.TabModel.Parent.PanelSize = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
                    hierarchyView.TabModel.Parent.Parent.PanelSize = new System.Windows.GridLength(500);

                    foreach (var well in existingToolWells)
                        well.TogglePinStatusCommand.Execute(null);

                    renderView.LoadScenario();
                }
                else
                {
                    var propertyView = new Controls.InstancePropertyView();
                    var renderView = new Controls.BspEditor();

                    var model = new StructureBspModel(modelTag)
                    {
                        PropertyView = propertyView,
                        RenderView = renderView,
                        LogError = LogError
                    };

                    var existingToolWells = EnumerateChildren(targetWindow.DockContainer)
                        .OfType<ToolWellModel>()
                        .ToList();

                    targetWindow.DocumentPanel.AddItem(renderView.TabModel);
                    targetWindow.DockContainer.AddTool(propertyView.TabModel, null, System.Windows.Controls.Dock.Right);

                    //the panels attempt to size to the contents but since the controls havent loaded yet
                    //they will have no size - we need to set a size for them.
                    propertyView.TabModel.Parent.PanelSize = new System.Windows.GridLength(500);

                    foreach (var well in existingToolWells)
                        well.TogglePinStatusCommand.Execute(null);

                    renderView.LoadStructureBsp();
                }

                LogOutput($"Loaded tag: {fileName}");
            }
            catch (Exception e)
            {
                LogError($"Error loading tag: {fileName}", e, true);
            }
        }
    }
}
