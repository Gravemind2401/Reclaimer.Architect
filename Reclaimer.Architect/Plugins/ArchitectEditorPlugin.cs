﻿using Adjutant.Blam.Common;
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
            return args.File.OfType<IIndexItem>().Any(i => Controls.DXEditor.CanOpenTag(i))
                || args.File.OfType<IIndexItem>().Any(i => Controls.BspEditor.CanOpenTag(i));
        }

        public override void OpenFile(OpenFileArgs args)
        {
            var modelTag = args.File.OfType<IIndexItem>().FirstOrDefault();
            DisplayModel(args.TargetWindow, modelTag, args.FileName);
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
                        Owner = System.Windows.Application.Current.MainWindow,
                        Title = "Reclaimer - Architect"
                    };

                    wnd.Show();
                    wnd.Activate();

                    wnd.Owner = null;

                    renderView.LoadScenario();
                }
                else
                {
                    var hierarchyView = new Controls.HierarchyView();
                    var propertyView = new Controls.InstancePropertyView();
                    var renderView = new Controls.BspEditor();

                    var model = new StructureBspModel(modelTag)
                    {
                        //HierarchyView = hierarchyView,
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
                        Owner = System.Windows.Application.Current.MainWindow,
                        Title = "Reclaimer - Architect"
                    };

                    wnd.Show();
                    wnd.Activate();

                    wnd.Owner = null;

                    renderView.LoadStructureBsp();
                }

                LogOutput($"Loaded tag: {fileName}");
            }
            catch (Exception e)
            {
                LogError($"Error loading tag: {fileName}", e);
            }
        }
    }
}