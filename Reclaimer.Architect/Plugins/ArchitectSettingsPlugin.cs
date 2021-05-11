using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Plugins
{
    public class ArchitectSettingsPlugin : Plugin
    {
        public override string Name => "Architect";

        //this needs to have a value at design time because the editor control uses it
        internal static ArchitectSettings Settings = new ArchitectSettings();

        public override void Initialise()
        {
            Settings = LoadSettings<ArchitectSettings>();

            foreach (var f in Directory.GetFiles($"{Substrate.PluginsDirectory}\\Architect", "*.dll"))
                Assembly.LoadFile(f);
        }

        public override void Suspend()
        {
            SaveSettings(Settings);
        }
    }

    internal class ArchitectSettings
    {
        [DisplayName("Default Save Format")]
        [Description("The initially selected file type when saving a model")]
        public string DefaultSaveFormat { get; set; }

        [DisplayName("Default FOV")]
        [Description("The starting camera field of view in the editor")]
        public double DefaultFieldOfView { get; set; }

        [DisplayName("Highlight Selection")]
        [Description("Highlight the currently selected object in the editor")]
        public bool HighlightSelection { get; set; }

        [DisplayName("Editor Translation")]
        [Description("Show the translation handles on the editor gizmo")]
        public bool EditorTranslation { get; set; }

        [DisplayName("Editor Rotation")]
        [Description("Show the rotation handles on the editor gizmo")]
        public bool EditorRotation { get; set; }

        [DisplayName("Editor Scaling")]
        [Description("Show the scale handles on the editor gizmo")]
        public bool EditorScaling { get; set; }

        [DisplayName("Editor Global Axes")]
        [Description("Toggle between global and local axis mode on the editor gizmo")]
        public bool EditorGlobalAxes { get; set; }

        public ArchitectSettings()
        {
            DefaultSaveFormat = "amf";
            DefaultFieldOfView = 45;

            HighlightSelection = true;
            EditorTranslation = EditorRotation = EditorScaling = true;
            EditorGlobalAxes = true;
        }
    }
}
