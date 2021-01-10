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
        LightFixtures,
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

        // AI
        AiSquadGroups,
        AiZones,
        AiZoneItem,
        AiFiringPositions,
        AiZoneAreas,
        AiSquads,
        AiSquadItem,
        AiEncounters, //base squads
        AiEncounterItem,
        AiStartingLocations,


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
