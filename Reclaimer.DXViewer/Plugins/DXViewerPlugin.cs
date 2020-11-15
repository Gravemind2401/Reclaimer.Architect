﻿using Adjutant.Blam.Common;
using Adjutant.Utilities;
using Reclaimer.Models;
using Reclaimer.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Plugins
{
    public class DXViewerPlugin : Plugin
    {
        public override string Name => "DXViewer";

        public override bool CanOpenFile(OpenFileArgs args)
        {
            return args.File.Any(i => i is IRenderGeometry)
                || args.File.OfType<IIndexItem>().Any(i => Controls.DXViewer.CanOpenTag(i))
                || args.File.OfType<IIndexItem>().Any(i => Controls.DXEditor.CanOpenTag(i));
        }

        public override void OpenFile(OpenFileArgs args)
        {
            var model = args.File.OfType<IRenderGeometry>().FirstOrDefault();
            if (model != null)
            {
                DisplayModel(args.TargetWindow, model, args.FileName);
                return;
            }

            var modelTag = args.File.OfType<IIndexItem>().FirstOrDefault();
            DisplayModel(args.TargetWindow, modelTag, args.FileName);
        }

        [SharedFunction]
        public void DisplayModel(ITabContentHost targetWindow, IRenderGeometry model, string fileName)
        {
            var container = targetWindow.DocumentPanel;

            LogOutput($"Loading model: {fileName}");

            try
            {
                var viewer = new Controls.DXViewer
                {
                    LogOutput = LogOutput,
                    LogError = LogError,
                    SetStatus = SetWorkingStatus,
                    ClearStatus = ClearWorkingStatus
                };

                viewer.LoadGeometry(model, $"{fileName}");

                container.AddItem(viewer.TabModel);

                LogOutput($"Loaded model: {fileName}");
            }
            catch (Exception e)
            {
                LogError($"Error loading model: {fileName}", e);
            }
        }

        [SharedFunction]
        public void DisplayModel(ITabContentHost targetWindow, IIndexItem modelTag, string fileName)
        {
            var container = targetWindow.DocumentPanel;

            LogOutput($"Loading model: {fileName}");

            try
            {
                if (Controls.DXEditor.CanOpenTag(modelTag))
                {
                    var hierarchyView = new Controls.HierarchyView();
                    var propertyView = new Controls.PropertyView();
                    var renderView = new Controls.DXEditor
                    {
                        LogOutput = LogOutput,
                        LogError = LogError,
                        SetStatus = SetWorkingStatus,
                        ClearStatus = ClearWorkingStatus
                    };

                    var model = new Models.ScenarioModel(modelTag)
                    {
                        HierarchyView = hierarchyView,
                        PropertyView = propertyView,
                        RenderView = renderView,
                        LogError = LogError
                    };

                    var layout = new DockContainerModel();
                    var docPanel = new DocumentPanelModel();

                    var mainSplit = new SplitPanelModel();
                    mainSplit.Item1 = docPanel;

                    var toolSplit = new SplitPanelModel { Orientation = System.Windows.Controls.Orientation.Vertical };
                    var tool1 = new ToolWellModel();
                    tool1.Children.Add(hierarchyView.TabModel);
                    var tool2 = new ToolWellModel();
                    tool2.Children.Add(propertyView.TabModel);
                    toolSplit.Item1 = tool1;
                    toolSplit.Item2 = tool2;

                    mainSplit.Item2 = toolSplit;
                    toolSplit.PanelSize = new System.Windows.GridLength(500);
                    docPanel.AddItem(renderView.TabModel);
                    layout.Content = mainSplit;

                    var wnd = new RaftedWindow(layout, docPanel)
                    {
                        Width = 1600,
                        Height = 900,
                        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                        Owner = System.Windows.Application.Current.MainWindow
                    };

                    wnd.Show();
                    wnd.Activate();

                    wnd.Owner = null;

                    renderView.LoadScenario();
                }
                else
                {
                    var viewer = new Controls.DXViewer
                    {
                        LogOutput = LogOutput,
                        LogError = LogError,
                        SetStatus = SetWorkingStatus,
                        ClearStatus = ClearWorkingStatus
                    };

                    viewer.LoadGeometry(modelTag, $"{fileName}");

                    container.AddItem(viewer.TabModel);
                }

                LogOutput($"Loaded model: {fileName}");
            }
            catch (Exception e)
            {
                LogError($"Error loading model: {fileName}", e);
            }
        }
    }
}
