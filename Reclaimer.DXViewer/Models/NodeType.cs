using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Models
{
    public enum NodeType
    {
        None,
        Mission,
        
        // Devices
        Machines,
        Controls,
        LightFixtrures,
        DeviceGroups,

        // Items
        Equipment,
        Weapons,

        // Units
        Bipeds,
        Vehicles,

        // Objects
        Crates,
        Scenery,
        SoundScenery,

        StartPositions,
        Comments,

        // Sandbox
        ForgeVehicles,
        ForgeWeapons,
        ForgeEquipment,
        ForgeScenery,
        ForgeTeleporters,
        ForgeGoalObjects,
        ForgeSpawning,

        // Game Data
        TriggerVolumes,
        CameraPoints,
        StartProfiles,
        Decals
    }
}
