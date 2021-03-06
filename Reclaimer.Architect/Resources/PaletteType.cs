﻿using Reclaimer.Models;
using Reclaimer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Resources
{
    public static class PaletteType
    {
        public const string Biped = "biped";
        public const string Vehicle = "vehicle";
        public const string Weapon = "weapon";
        public const string Equipment = "equipment";
        public const string Scenery = "scenery";
        public const string Machine = "machine";
        public const string Control = "control";
        public const string Crate = "crate";
        public const string LightFixture = "lightfixture";
        public const string Decal = "decal";

        private static Dictionary<NodeType, string> ByNodeType = new Dictionary<NodeType, string>
        {
            { NodeType.Scenery, PaletteType.Scenery },
            { NodeType.Bipeds, PaletteType.Biped },
            { NodeType.Vehicles, PaletteType.Vehicle },
            { NodeType.Equipment, PaletteType.Equipment },
            { NodeType.Weapons, PaletteType.Weapon },
            { NodeType.Machines, PaletteType.Machine },
            { NodeType.Controls, PaletteType.Control },
            { NodeType.LightFixtures, PaletteType.LightFixture },
            { NodeType.Crates, PaletteType.Crate },
            { NodeType.Decals, PaletteType.Decal }
        };

        public static string FromNodeType(NodeType type) => ByNodeType.ValueOrDefault(type);
    }
}
