using Adjutant.Blam.Common;
using Adjutant.Geometry;
using Adjutant.Spatial;
using Adjutant.Utilities;
using Reclaimer.Models;
using Reclaimer.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Dds;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static HelixToolkit.Wpf.SharpDX.Media3DExtension;

using Media = System.Windows.Media;
using Media3D = System.Windows.Media.Media3D;
using Helix = HelixToolkit.Wpf.SharpDX;
using System.Windows.Data;
using Reclaimer.Controls;
using System.Collections.Concurrent;
using Reclaimer.Resources;
using Reclaimer.Models.Ai;
using Reclaimer.Controls.Markers;

namespace Reclaimer.Geometry
{
    public class SceneManager : IDisposable
    {
        private readonly ModelFactory factory = new ModelFactory();

        private ScenarioModel scenario;
        public ObjectHolder BspHolder { get; private set; }
        public ObjectHolder SkyHolder { get; private set; }
        public Dictionary<string, PaletteHolder> PaletteHolders { get; private set; }

        public Helix.GroupElement3D StartPositionGroup { get; private set; }
        public Helix.GroupElement3D TriggerVolumeGroup { get; private set; }
        public ObservableCollection<StartPositionMarker3D> StartPositions { get; private set; }
        public ObservableCollection<BoxManipulator3D> TriggerVolumes { get; private set; }

        public Dictionary<AiZone, Helix.GroupElement3D> AiAreaGroups { get; private set; }
        public Dictionary<AiZone, ObservableCollection<PositionMarker3D>> AiAreas { get; private set; }
        public Dictionary<AiZone, Helix.GroupElement3D> AiFiringPositionGroups { get; private set; }
        public Dictionary<AiZone, ObservableCollection<PositionMarker3D>> AiFiringPositions { get; private set; }
        public Dictionary<AiEncounter, Helix.GroupElement3D> AiStartLocationGroups { get; private set; }
        public Dictionary<AiEncounter, ObservableCollection<SpawnPointMarker3D>> AiStartLocations { get; private set; }

        public Dictionary<AiSquad, Helix.GroupElement3D> AiGroupStartLocationGroups { get; private set; }
        public Dictionary<AiSquad, ObservableCollection<SpawnPointMarker3D>> AiGroupStartLocations { get; private set; }
        public Dictionary<AiSquad, Helix.GroupElement3D> AiSoloStartLocationGroups { get; private set; }
        public Dictionary<AiSquad, ObservableCollection<SpawnPointMarker3D>> AiSoloStartLocations { get; private set; }

        public IEnumerable<Task> ReadScenario(ScenarioModel scenario)
        {
            this.scenario = scenario;
            BspHolder = new ObjectHolder("sbsp");
            SkyHolder = new ObjectHolder("sky");
            PaletteHolders = new Dictionary<string, PaletteHolder>();
            StartPositions = new ObservableCollection<StartPositionMarker3D>();
            TriggerVolumes = new ObservableCollection<BoxManipulator3D>();

            AiAreaGroups = new Dictionary<AiZone, Helix.GroupElement3D>();
            AiAreas = new Dictionary<AiZone, ObservableCollection<PositionMarker3D>>();
            AiFiringPositionGroups = new Dictionary<AiZone, Helix.GroupElement3D>();
            AiFiringPositions = new Dictionary<AiZone, ObservableCollection<PositionMarker3D>>();
            AiStartLocationGroups = new Dictionary<AiEncounter, Helix.GroupElement3D>();
            AiStartLocations = new Dictionary<AiEncounter, ObservableCollection<SpawnPointMarker3D>>();

            AiGroupStartLocationGroups = new Dictionary<AiSquad, Helix.GroupElement3D>();
            AiGroupStartLocations = new Dictionary<AiSquad, ObservableCollection<SpawnPointMarker3D>>();
            AiSoloStartLocationGroups = new Dictionary<AiSquad, Helix.GroupElement3D>();
            AiSoloStartLocations = new Dictionary<AiSquad, ObservableCollection<SpawnPointMarker3D>>();

            yield return Task.Run(() =>
            {
                for (int i = 0; i < scenario.Bsps.Count; i++)
                    factory.LoadTag(scenario.Bsps[i].Tag, false);
            });

            yield return Task.Run(() =>
            {
                for (int i = 0; i < scenario.Skies.Count; i++)
                    factory.LoadTag(scenario.Skies[i].Tag, false);
            });

            foreach (var definition in scenario.Palettes.Values)
            {
                //not fully implemented yet
                if (definition.Name == PaletteType.LightFixture)
                    continue;

                var holder = new PaletteHolder(definition);
                PaletteHolders.Add(holder.Name, holder);

                yield return Task.Run(() =>
                {
                    for (int i = 0; i < definition.Palette.Count; i++)
                        if (ModelFactory.IsTagSupported(definition.Palette[i].Tag))
                            factory.LoadTag(definition.Palette[i].Tag, false);
                });
            }
        }

        public void RenderScenario()
        {
            if (scenario == null)
                throw new InvalidOperationException();

            foreach (var bsp in scenario.Bsps)
                BspHolder.Elements.Add(bsp.Tag == null ? null : factory.CreateRenderModel(bsp.Tag.Id));

            foreach (var sky in scenario.Skies)
                SkyHolder.Elements.Add(sky.Tag == null ? null : factory.CreateObjectModel(sky.Tag.Id));

            foreach (var holder in PaletteHolders.Values)
            {
                holder.GroupElement = new Helix.GroupModel3D();
                holder.SetCapacity(holder.Definition.Placements.Count);

                for (int i = 0; i < holder.Definition.Placements.Count; i++)
                    ConfigurePlacement(holder, i);
            }

            StartPositionGroup = new Helix.GroupModel3D();
            foreach (var pos in scenario.StartingPositions)
            {
                var element = new StartPositionMarker3D();
                BindStartPosition(pos, element);
                StartPositions.Add(element);
                StartPositionGroup.Children.Add(element);
            }

            TriggerVolumeGroup = new Helix.GroupModel3D();
            foreach (var vol in scenario.TriggerVolumes)
            {
                var box = new BoxManipulator3D
                {
                    DiffuseColor = TriggerVolume.DefaultColour,
                    Position = ((IRealVector3D)vol.Position).ToVector3(),
                    Size = ((IRealVector3D)vol.Size).ToVector3()
                };

                BindTriggerVolume(vol, box);

                TriggerVolumes.Add(box);
                TriggerVolumeGroup.Children.Add(box);
            }

            foreach (var zone in scenario.SquadHierarchy.Zones)
            {
                var areaGroup = new Helix.GroupModel3D();
                var areaMarkers = new ObservableCollection<PositionMarker3D>();

                foreach (var area in zone.Areas)
                {
                    var areaMarker = new PositionMarker3D();
                    BindZoneArea(area, areaMarker);
                    areaMarkers.Add(areaMarker);
                    areaGroup.Children.Add(areaMarker);
                }

                AiAreaGroups.Add(zone, areaGroup);
                AiAreas.Add(zone, areaMarkers);

                var fposGroup = new Helix.GroupModel3D();
                var fposMarkers = new ObservableCollection<PositionMarker3D>();

                foreach (var area in zone.FiringPositions)
                {
                    var fposMarker = new PositionMarker3D();
                    BindZoneFiringPosition(area, fposMarker);
                    fposMarkers.Add(fposMarker);
                    fposGroup.Children.Add(fposMarker);
                }

                AiFiringPositionGroups.Add(zone, fposGroup);
                AiFiringPositions.Add(zone, fposMarkers);

                foreach (var squad in zone.Squads)
                {
                    Helix.GroupModel3D locGroup;
                    ObservableCollection<SpawnPointMarker3D> locMarkers;

                    #region Encounter Starting Locations
                    foreach (var enc in squad.Encounters)
                    {
                        locGroup = new Helix.GroupModel3D();
                        locMarkers = new ObservableCollection<SpawnPointMarker3D>();

                        foreach (var loc in enc.StartingLocations)
                        {
                            var locMarker = new SpawnPointMarker3D();
                            BindAiStartLocation(loc, locMarker);
                            locMarkers.Add(locMarker);
                            locGroup.Children.Add(locMarker);
                        }

                        AiStartLocationGroups.Add(enc, locGroup);
                        AiStartLocations.Add(enc, locMarkers);
                    }
                    #endregion

                    #region Group Starting Locations
                    locGroup = new Helix.GroupModel3D();
                    locMarkers = new ObservableCollection<SpawnPointMarker3D>();

                    foreach (var loc in squad.GroupStartLocations)
                    {
                        var locMarker = new SpawnPointMarker3D();
                        BindAiStartLocation(loc, locMarker);
                        locMarkers.Add(locMarker);
                        locGroup.Children.Add(locMarker);
                    }

                    AiGroupStartLocationGroups.Add(squad, locGroup);
                    AiGroupStartLocations.Add(squad, locMarkers);
                    #endregion

                    #region Single Starting Locations
                    locGroup = new Helix.GroupModel3D();
                    locMarkers = new ObservableCollection<SpawnPointMarker3D>();

                    foreach (var loc in squad.SoloStartLocations)
                    {
                        var locMarker = new SpawnPointMarker3D();
                        BindAiStartLocation(loc, locMarker);
                        locMarkers.Add(locMarker);
                        locGroup.Children.Add(locMarker);
                    }

                    AiSoloStartLocationGroups.Add(squad, locGroup);
                    AiSoloStartLocations.Add(squad, locMarkers);
                    #endregion
                }
            }
        }

        private void RemovePlacement(PaletteHolder holder, int index)
        {
            var element = holder.Elements[index];
            if (element == null)
                return;

            holder.GroupElement.Children.Remove(element);
            element.Dispose();
            holder.Elements[index] = null;
        }

        private void ConfigurePlacement(PaletteHolder holder, int index)
        {
            RemovePlacement(holder, index);

            var placement = holder.Definition.Placements[index];
            var tag = placement.PaletteIndex >= 0 ? holder.Definition.Palette[placement.PaletteIndex].Tag : null;
            if (tag == null)
            {
                holder.Elements[index] = null;
                return;
            }

            Helix.Element3D inst;
            if (holder.Definition.Name == PaletteType.LightFixture)
                inst = new LightMarker3D();
            else
            {
                inst = factory.CreateObjectModel(tag.Id);
                if (inst == null)
                {
                    holder.Elements[index] = null;
                    return;
                }
            }

            BindPlacement(placement, inst);

            holder.Elements[index] = inst;
            holder.GroupElement.Children.Add(inst);
        }

        public void RefreshPalette(string paletteKey, int index)
        {
            var holder = PaletteHolders[paletteKey];
            factory.LoadTag(holder.Definition.Palette[index].Tag, false); // in case it is new to the palette
            foreach (var placement in holder.Definition.Placements.Where(p => p.PaletteIndex == index))
                RefreshObject(paletteKey, placement, FieldId.PaletteIndex);
        }

        public void RefreshObject(string paletteKey, ObjectPlacement placement, string fieldId)
        {
            var holder = PaletteHolders[paletteKey];
            var index = holder.Definition.Placements.IndexOf(placement);

            if (fieldId == FieldId.Variant)
                (holder.Elements[index] as ObjectModel3D)?.SetVariant(placement.Variant);
            else if (fieldId == FieldId.PaletteIndex)
            {
                ConfigurePlacement(holder, index);

                var info = holder.GetInfoForIndex(index);
                info.TreeItem.Header = info.Placement.GetDisplayName();
                info.TreeItem.Tag = info.Element;

                var listItem = scenario.Items.FirstOrDefault(i => i.Tag == info.Placement);
                if (listItem != null)
                    listItem.Content = info.TreeItem.Header;
            }
        }

        private void BindPlacement(ObjectPlacement placement, Helix.Element3D model)
        {
            var binding = new MultiBinding { Converter = EulerTransformConverter.Instance, Mode = BindingMode.TwoWay };
            binding.Bindings.Add(new Binding(nameof(ObjectPlacement.Position)) { Mode = BindingMode.TwoWay });
            binding.Bindings.Add(new Binding(nameof(ObjectPlacement.Rotation)) { Mode = BindingMode.TwoWay });
            binding.Bindings.Add(new Binding(nameof(ObjectPlacement.Scale)) { Mode = BindingMode.TwoWay });

            model.DataContext = placement;
            BindingOperations.SetBinding(model, Helix.Element3D.TransformProperty, binding);
        }

        private void BindStartPosition(StartPosition pos, Helix.Element3D model)
        {
            var binding = new MultiBinding { Converter = EulerTransformConverter.Instance, Mode = BindingMode.TwoWay };
            binding.Bindings.Add(new Binding(nameof(StartPosition.Position)) { Mode = BindingMode.TwoWay });
            binding.Bindings.Add(new Binding(nameof(StartPosition.Orientation)) { Mode = BindingMode.TwoWay });

            model.DataContext = pos;
            BindingOperations.SetBinding(model, Helix.Element3D.TransformProperty, binding);
        }

        private void BindTriggerVolume(TriggerVolume vol, BoxManipulator3D box)
        {
            box.DataContext = vol;
            BindingOperations.SetBinding(box, BoxManipulator3D.PositionProperty,
                new Binding(nameof(TriggerVolume.Position)) { Mode = BindingMode.TwoWay, Converter = SharpDXVectorConverter.Instance });
            BindingOperations.SetBinding(box, BoxManipulator3D.SizeProperty,
                new Binding(nameof(TriggerVolume.Size)) { Mode = BindingMode.TwoWay, Converter = SharpDXVectorConverter.Instance });
        }

        private void BindZoneFiringPosition(AiFiringPosition fpos, Helix.Element3D model)
        {
            var binding = new Binding(nameof(AiFiringPosition.Position)) { Converter = TranslationTransformConverter.Instance, Mode = BindingMode.TwoWay };

            model.DataContext = fpos;
            BindingOperations.SetBinding(model, Helix.Element3D.TransformProperty, binding);
        }

        private void BindZoneArea(AiArea area, Helix.Element3D model)
        {
            var binding = new Binding(nameof(AiArea.Position)) { Converter = TranslationTransformConverter.Instance, Mode = BindingMode.TwoWay };

            model.DataContext = area;
            BindingOperations.SetBinding(model, Helix.Element3D.TransformProperty, binding);
        }

        private void BindAiStartLocation(AiStartingLocation pos, Helix.Element3D model)
        {
            var binding = new MultiBinding { Converter = EulerTransformConverter.Instance, Mode = BindingMode.TwoWay };
            binding.Bindings.Add(new Binding(nameof(AiStartingLocation.Position)) { Mode = BindingMode.TwoWay });
            binding.Bindings.Add(new Binding(nameof(AiStartingLocation.Rotation)) { Mode = BindingMode.TwoWay });

            model.DataContext = pos;
            BindingOperations.SetBinding(model, Helix.Element3D.TransformProperty, binding);
        }

        public void Dispose()
        {
            BspHolder?.Dispose();
            SkyHolder?.Dispose();

            if (PaletteHolders != null)
            {
                foreach (var holder in PaletteHolders.Values)
                    holder.Dispose();
            }

            factory.Dispose();
        }
    }
}
