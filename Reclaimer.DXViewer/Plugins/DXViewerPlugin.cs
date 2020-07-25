using Adjutant.Utilities;
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
            return args.File.Any(i => i is IRenderGeometry);
        }

        public override void OpenFile(OpenFileArgs args)
        {
            var model = args.File.OfType<IRenderGeometry>().FirstOrDefault();
            DisplayModel(args.TargetWindow, model, args.FileName);
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
    }
}
