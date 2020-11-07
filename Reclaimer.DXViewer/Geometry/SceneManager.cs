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

namespace Reclaimer.Geometry
{
    public class SceneManager : IDisposable
    {
        private readonly ModelFactory factory = new ModelFactory();

        private ScenarioModel scenario;
        public ObjectHolder BspHolder { get; private set; }
        public ObjectHolder SkyHolder { get; private set; }
        public Dictionary<string, PaletteHolder> PaletteHolders { get; private set; }

        public Helix.GroupElement3D TriggerVolumeGroup { get; private set; }
        public ObservableCollection<BoxManipulator3D> TriggerVolumes { get; private set; }

        public IEnumerable<Task> ReadScenario(ScenarioModel scenario)
        {
            this.scenario = scenario;
            BspHolder = new ObjectHolder("sbsp");
            SkyHolder = new ObjectHolder("sky");
            PaletteHolders = new Dictionary<string, PaletteHolder>();
            TriggerVolumes = new ObservableCollection<BoxManipulator3D>();

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
                var holder = new PaletteHolder(definition);
                PaletteHolders.Add(holder.Name, holder);

                yield return Task.Run(() =>
                {
                    for (int i = 0; i < definition.Palette.Count; i++)
                        factory.LoadTag(definition.Palette[i].Tag, false);
                });
            }
        }

        public void RenderScenario()
        {
            if (scenario == null)
                throw new InvalidOperationException();

            foreach (var bsp in scenario.Bsps)
                BspHolder.Elements.Add(factory.CreateRenderModel(bsp.Tag.Id));

            foreach (var sky in scenario.Skies)
                SkyHolder.Elements.Add(factory.CreateObjectModel(sky.Tag.Id));

            foreach (var holder in PaletteHolders.Values)
            {
                holder.GroupElement = new Helix.GroupModel3D();
                for (int i = 0; i < holder.Definition.Placements.Count; i++)
                {
                    var placement = holder.Definition.Placements[i];
                    var tag = placement.PaletteIndex >= 0 ? holder.Definition.Palette[placement.PaletteIndex].Tag : null;
                    if (tag == null)
                    {
                        //need to keep elements at the correct index to match their corresponding block index
                        holder.Elements.Add(null);
                        continue;
                    }

                    var inst = factory.CreateObjectModel(tag.Id);
                    if (inst == null)
                    {
                        holder.Elements.Add(null);
                        continue;
                    }

                    var binding = new MultiBinding { Converter = TransformConverter.Instance, Mode = BindingMode.TwoWay };
                    binding.Bindings.Add(new Binding(nameof(ObjectPlacement.Position)) { Mode = BindingMode.TwoWay });
                    binding.Bindings.Add(new Binding(nameof(ObjectPlacement.Rotation)) { Mode = BindingMode.TwoWay });
                    binding.Bindings.Add(new Binding(nameof(ObjectPlacement.Scale)) { Mode = BindingMode.TwoWay });

                    inst.DataContext = placement;
                    BindingOperations.SetBinding(inst, Helix.Element3D.TransformProperty, binding);

                    holder.Elements.Add(inst);
                    holder.GroupElement.Children.Add(inst);
                }
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

                box.DataContext = vol;
                BindingOperations.SetBinding(box, BoxManipulator3D.PositionProperty,
                    new Binding(nameof(TriggerVolume.Position)) { Mode = BindingMode.TwoWay, Converter = SharpDXVectorConverter.Instance });
                BindingOperations.SetBinding(box, BoxManipulator3D.SizeProperty,
                    new Binding(nameof(TriggerVolume.Size)) { Mode = BindingMode.TwoWay, Converter = SharpDXVectorConverter.Instance });

                TriggerVolumes.Add(box);
                TriggerVolumeGroup.Children.Add(box);
            }
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
