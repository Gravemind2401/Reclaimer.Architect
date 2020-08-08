using Adjutant.Blam.Common;
using Adjutant.Spatial;
using System;
using System.Collections.Generic;
using System.IO.Endian;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Blam.Halo3
{
    public class scenario
    {
        [Offset(20)]
        public BlockCollection<StructureBsp> StructureBsps { get; set; }

        [Offset(48)]
        public BlockCollection<SkyReference> SkyReferences { get; set; }

        [Offset(60)]
        public BlockCollection<BspGroup> BspGroups { get; set; }

        [Offset(84)]
        public BlockCollection<ZonesetGroup> ZonesetGroups { get; set; }

        [Offset(176)]
        public BlockCollection<ObjectName> ObjectNames { get; set; }

        [Offset(188)]
        public BlockCollection<SceneryPlacement> SceneryPlacements { get; set; }

        [Offset(200)]
        public BlockCollection<PaletteItem> SceneryPalette { get; set; }

        [Offset(212)]
        public BlockCollection<BipedPlacement> BipedPlacements { get; set; }

        [Offset(224)]
        public BlockCollection<PaletteItem> BipedPalette { get; set; }

        [Offset(236)]
        public BlockCollection<VehiclePlacement> VehiclePlacements { get; set; }

        [Offset(248)]
        public BlockCollection<PaletteItem> VehiclePalette { get; set; }

        [Offset(260)]
        public BlockCollection<EquipmentPlacement> EquipmentPlacements { get; set; }

        [Offset(272)]
        public BlockCollection<PaletteItem> EquipmentPalette { get; set; }

        [Offset(284)]
        public BlockCollection<WeaponPlacement> WeaponPlacements { get; set; }

        [Offset(296)]
        public BlockCollection<PaletteItem> WeaponPalette { get; set; }

        [Offset(320)]
        public BlockCollection<MachinePlacement> MachinePlacements { get; set; }

        [Offset(332)]
        public BlockCollection<PaletteItem> MachinePalette { get; set; }

        [Offset(596)]
        public BlockCollection<StartingLocation> StartingLocations { get; set; }

        [Offset(608)]
        public BlockCollection<TriggerVolume> TriggerVolumes { get; set; }

        [Offset(1512)]
        public BlockCollection<CratePlacement> CratePlacements { get; set; }

        [Offset(1524)]
        public BlockCollection<PaletteItem> CratePalette { get; set; }
    }

    [FixedSize(108)]
    public class StructureBsp
    {
        [Offset(0)]
        public TagReference BspReference { get; set; }

        public override string ToString() => BspReference.ToString();
    }

    [FixedSize(20)]
    public class SkyReference
    {
        [Offset(0)]
        public TagReference SkyObjectReference { get; set; }

        [Offset(16)]
        public short NameIndex { get; set; }

        [Offset(18)]
        public short ActiveBsps { get; set; }

        public override string ToString() => SkyObjectReference.ToString();
    }

    [FixedSize(44)]
    public class BspGroup
    {
        [Offset(0)]
        public int IncludedBsps { get; set; }
    }

    [FixedSize(36)]
    public class ZonesetGroup
    {
        [Offset(0)]
        public StringId Name { get; set; }

        [Offset(4)]
        public int BspGroupIndex { get; set; }

        [Offset(12)]
        public int LoadedBsps { get; set; }

        [Offset(16)]
        public int LoadedDesignerSets { get; set; }

        [Offset(20)]
        public int UnloadedDesignerSets { get; set; }

        public override string ToString() => Name;
    }

    [FixedSize(36)]
    public class ObjectName
    {
        [Offset(0)]
        [NullTerminated(Length = 32)]
        public string Name { get; set; }

        [Offset(32)]
        public short Type { get; set; }

        [Offset(34)]
        public short PlacementIndex { get; set; }

        public override string ToString() => Name;
    }

    [FixedSize(48)]
    public class PaletteItem
    {
        [Offset(0)]
        public TagReference ObjectReference { get; set; }

        public override string ToString() => ObjectReference.ToString();
    }

    [FixedSize(180)]
    public class SceneryPlacement : ObjectPlacement
    {
        [Offset(84)]
        public VariantPlacementData VariantData { get; set; }
    }

    [FixedSize(116)]
    public class BipedPlacement : ObjectPlacement
    {
        [Offset(84)]
        public VariantPlacementData VariantData { get; set; }
    }

    [FixedSize(168)]
    public class VehiclePlacement : ObjectPlacement
    {
        [Offset(84)]
        public VariantPlacementData VariantData { get; set; }
    }

    [FixedSize(140)]
    public class EquipmentPlacement : ObjectPlacement
    {

    }

    [FixedSize(168)]
    public class WeaponPlacement : ObjectPlacement
    {
        [Offset(84)]
        public VariantPlacementData VariantData { get; set; }

        [Offset(108)]
        public WeaponPlacementData WeaponData { get; set; }
    }

    [FixedSize(112)]
    public class MachinePlacement : ObjectPlacement
    {

    }

    [FixedSize(176)]
    public class CratePlacement : ObjectPlacement
    {
        [Offset(84)]
        public VariantPlacementData VariantData { get; set; }
    }

    [FixedSize(24)]
    public class StartingLocation
    {
        [Offset(0)]
        public RealVector3D Position { get; set; }

        [Offset(12)]
        public RealVector2D Facing { get; set; }

        [Offset(22)]
        public short PlayerType { get; set; }
    }

    [FixedSize(68)]
    public class TriggerVolume
    {
        [Offset(0)]
        public StringId Name { get; set; }

        [Offset(4)]
        public short ObjectNameIndex { get; set; }

        [Offset(8)]
        public StringId NodeName { get; set; }

        [Offset(36)]
        public RealVector3D Position { get; set; }

        [Offset(48)]
        public RealVector3D Size { get; set; }

        public override string ToString() => Name;
    }

    [FixedSize(84)]
    public class ObjectPlacement
    {
        [Offset(0)]
        public short PaletteIndex { get; set; }

        [Offset(2)]
        public short NameIndex { get; set; }

        [Offset(4)]
        public int PlacementFlags { get; set; }

        [Offset(8)]
        public RealVector3D Position { get; set; }

        [Offset(20)]
        public RealVector3D Rotation { get; set; }

        [Offset(32)]
        public float Scale { get; set; }

        [Offset(62)]
        public byte ObjectType { get; set; }

        [Offset(80)]
        public short AllowedZonesets { get; set; }
    }

    [FixedSize(64)]
    public struct VariantPlacementData
    {
        [Offset(0)]
        public StringId Variant { get; set; }

        [Offset(4)]
        public int ActiveColourChanges { get; set; }

        [Offset(8)]
        public int PrimaryColour { get; set; }

        [Offset(12)]
        public int SecondarColour { get; set; }

        [Offset(16)]
        public int TertiaryColour { get; set; }

        [Offset(24)]
        public int QuaternaryColour { get; set; }

        public override string ToString() => Variant;
    }

    [FixedSize(8)]
    public struct WeaponPlacementData
    {
        [Offset(0)]
        public short RoundsLeft { get; set; }

        [Offset(2)]
        public short RoundsLoaded { get; set; }

        [Offset(4)]
        public int WeaponFlags { get; set; }
    }
}
