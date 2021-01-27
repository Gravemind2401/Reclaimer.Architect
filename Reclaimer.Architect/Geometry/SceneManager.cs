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
        public Dictionary<string, PaletteHolder> PaletteHolders { get; private set; }

        public IEnumerable<Task> ReadScenario(ScenarioModel scenario)
        {
            this.scenario = scenario;
            PaletteHolders = new Dictionary<string, PaletteHolder>();

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

            foreach (var c in scenario.ComponentManagers)
                yield return c.InitializeResourcesAsync(factory);
        }

        public void RenderScenario()
        {
            if (scenario == null)
                throw new InvalidOperationException();

            foreach (var holder in PaletteHolders.Values)
            {
                holder.GroupElement = new Helix.GroupModel3D();
                holder.SetCapacity(holder.Definition.Placements.Count);

                for (int i = 0; i < holder.Definition.Placements.Count; i++)
                    ConfigurePlacement(holder, i);
            }

            foreach (var c in scenario.ComponentManagers)
                c.InitializeElements(factory);
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
            if (holder.Definition.Name == PaletteType.Decal)
                inst = new DecalMarker3D();
            else if (holder.Definition.Name == PaletteType.LightFixture)
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
            IMultiValueConverter converter = EulerTransformConverter.Instance;
            var rotationPath = nameof(ObjectPlacement.Rotation);

            if (placement.PaletteKey == PaletteType.Decal)
            {
                converter = QuaternionTransformConverter.Instance;
                rotationPath = nameof(ObjectPlacement.QRotation);
            }

            var binding = new MultiBinding { Converter = converter, Mode = BindingMode.TwoWay };
            binding.Bindings.Add(new Binding(nameof(ObjectPlacement.Position)) { Mode = BindingMode.TwoWay });
            binding.Bindings.Add(new Binding(rotationPath) { Mode = BindingMode.TwoWay });
            binding.Bindings.Add(new Binding(nameof(ObjectPlacement.Scale)) { Mode = BindingMode.TwoWay });

            model.DataContext = placement;
            BindingOperations.SetBinding(model, Helix.Element3D.TransformProperty, binding);
        }

        public void Dispose()
        {
            foreach (var c in scenario.ComponentManagers)
                c.DisposeSceneElements();

            if (PaletteHolders != null)
            {
                foreach (var holder in PaletteHolders.Values)
                    holder.Dispose();
            }

            factory.Dispose();
        }
    }
}
