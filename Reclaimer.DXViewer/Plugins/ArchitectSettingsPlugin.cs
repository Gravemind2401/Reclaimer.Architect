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
    public class ArchitectSettingsPlugin : Plugin
    {
        public override string Name => "Architect";

        //this needs to have a value at design time because the editor control uses it
        internal static DXViewerSettings Settings = new DXViewerSettings();

        public override void Initialise()
        {
            Settings = LoadSettings<DXViewerSettings>();

            foreach (var f in Directory.GetFiles($"{Substrate.PluginsDirectory}\\Architect", "*.dll"))
                Assembly.LoadFile(f);
        }

        public override void Suspend()
        {
            SaveSettings(Settings);
        }
    }

    internal class DXViewerSettings
    {
        public string DefaultSaveFormat { get; set; }
        public double DefaultFieldOfView { get; set; }
        public bool EditorTranslation { get; set; }
        public bool EditorRotation { get; set; }
        public bool EditorScaling { get; set; }
        public bool EditorGlobalAxes { get; set; }

        public DXViewerSettings()
        {
            DefaultSaveFormat = "amf";
            DefaultFieldOfView = 45;

            EditorTranslation = EditorRotation = EditorScaling = true;
            EditorGlobalAxes = true;
        }
    }
}
