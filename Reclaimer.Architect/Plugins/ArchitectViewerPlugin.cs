using Adjutant.Blam.Common;
using Adjutant.Utilities;
using Reclaimer.Annotations;
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
    public class ArchitectViewerPlugin : Plugin
    {
        public override string Name => "Architect Viewer";

        public override bool CanOpenFile(OpenFileArgs args)
        {
            return args.File.Any(i => i is IRenderGeometry)
                || args.File.OfType<IIndexItem>().Any(i => Controls.DXViewer.CanOpenTag(i));
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
                var viewer = new Controls.DXViewer
                {
                    LogOutput = LogOutput,
                    LogError = LogError,
                    SetStatus = SetWorkingStatus,
                    ClearStatus = ClearWorkingStatus
                };

                viewer.LoadGeometry(modelTag, $"{fileName}");

                container.AddItem(viewer.TabModel);

                LogOutput($"Loaded model: {fileName}");
            }
            catch (Exception e)
            {
                LogError($"Error loading model: {fileName}", e);
            }
        }
    }
}
