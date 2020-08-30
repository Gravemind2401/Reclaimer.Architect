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

namespace Reclaimer.Geometry
{
    public class SceneManager : IDisposable
    {
        private static readonly Helix.Material ErrorMaterial = Helix.DiffuseMaterials.Gold;

        private readonly Dictionary<int, Helix.TextureModel> sceneTextures = new Dictionary<int, Helix.TextureModel>();

        private ScenarioModel scenario;
        public ObjectHolder BspHolder { get; private set; }
        public CompositeObjectHolder SkyHolder { get; private set; }
        public Dictionary<string, PaletteHolder> PaletteHolders { get; private set; }

        public void ReadScenario(ScenarioModel scenario)
        {
            this.scenario = scenario;
            BspHolder = new ObjectHolder();
            SkyHolder = new CompositeObjectHolder();
            PaletteHolders = new Dictionary<string, PaletteHolder>();

            for (int i = 0; i < scenario.Bsps.Count; i++)
            {
                IRenderGeometry geom;
                if (ContentFactory.TryGetGeometryContent(scenario.Bsps[i].Tag, out geom))
                    BspHolder.Managers.Add(new ModelManager(this, geom.ReadGeometry(0)));
                else BspHolder.Managers.Add(null);

                BspHolder.Managers[i]?.PreloadTextures();
            }

            for (int i = 0; i < scenario.Skies.Count; i++)
            {
                CompositeGeometryModel geom;
                if (CompositeModelFactory.TryGetModel(scenario.Skies[i].Tag, out geom))
                    SkyHolder.Managers.Add(new CompositeModelManager(this, geom));
                else SkyHolder.Managers.Add(null);

                SkyHolder.Managers[i]?.PreloadTextures();
            }

            foreach (var definition in scenario.Palettes.Values)
            {
                var holder = new PaletteHolder(definition);
                PaletteHolders.Add(holder.Name, holder);

                for (int i = 0; i < definition.Palette.Count; i++)
                {
                    CompositeGeometryModel geom;
                    if (CompositeModelFactory.TryGetModel(definition.Palette[i].Tag, out geom))
                        holder.Managers.Add(new CompositeModelManager(this, geom));
                    else holder.Managers.Add(null);

                    holder.Managers[i]?.PreloadTextures();
                }
            }
        }

        public void RenderScenario()
        {
            if (scenario == null)
                throw new InvalidOperationException();

            foreach (var man in BspHolder.Managers)
                BspHolder.Instances.Add(man?.GenerateModel());

            foreach (var man in SkyHolder.Managers)
                SkyHolder.Instances.Add(man?.GenerateModel());

            foreach (var holder in PaletteHolders.Values)
            {
                holder.GroupElement = new Helix.GroupModel3D();
                for (int i = 0; i < holder.Definition.Placements.Count; i++)
                {
                    var placement = holder.Definition.Placements[i];
                    var manager = placement.PaletteIndex >= 0 ? holder.Managers[placement.PaletteIndex] : null;
                    if (manager == null)
                    {
                        holder.Instances.Add(null);
                        continue;
                    }

                    var inst = manager.GenerateModel();
                    inst.Name = placement.GetDisplayName();

                    var binding = new MultiBinding { Converter = TransformConverter.Instance, Mode = BindingMode.TwoWay };
                    binding.Bindings.Add(new Binding(nameof(ObjectPlacement.Position)) { Mode = BindingMode.TwoWay });
                    binding.Bindings.Add(new Binding(nameof(ObjectPlacement.Rotation)) { Mode = BindingMode.TwoWay });
                    binding.Bindings.Add(new Binding(nameof(ObjectPlacement.Scale)) { Mode = BindingMode.TwoWay });

                    inst.Element.DataContext = placement;
                    BindingOperations.SetBinding(inst.Element, Helix.Element3D.TransformProperty, binding);

                    holder.Instances.Add(inst);
                    holder.GroupElement.Children.Add(inst.Element);
                }
            }
        }

        #region Materials
        public Helix.Material LoadMaterial(IGeometryModel model, int matIndex, out bool isTransparent)
        {
            if (matIndex < 0 || matIndex >= model.Materials.Count)
            {
                isTransparent = false;
                return ErrorMaterial;
            }

            return LoadMaterial(model.Materials[matIndex], out isTransparent);
        }

        public Helix.Material LoadMaterial(IGeometryMaterial mat, out bool isTransparent)
        {
            var diffuseTexture = GetTexture(mat, MaterialUsage.Diffuse);
            if (diffuseTexture == null)
            {
                isTransparent = false;
                return ErrorMaterial;
            }

            var material = new Helix.DiffuseMaterial
            {
                DiffuseMap = diffuseTexture
            };

            material.Freeze();
            isTransparent = mat.Flags.HasFlag(MaterialFlags.Transparent);
            return material;
        }

        public void LoadTexture(IGeometryMaterial mat)
        {
            GetTexture(mat, MaterialUsage.Diffuse);
            //GetTexture(mat, MaterialUsage.Normal);
        }

        private Helix.TextureModel GetTexture(IGeometryMaterial mat, MaterialUsage usage)
        {
            var sub = mat?.Submaterials.FirstOrDefault(s => s.Usage == MaterialUsage.Diffuse);
            if (string.IsNullOrEmpty(sub?.Bitmap?.Name))
                return null;

            var key = mat.Flags.HasFlag(MaterialFlags.Transparent) ? -sub.Bitmap.Id : sub.Bitmap.Id;
            var tex = sceneTextures.ValueOrDefault(key);
            if (tex == null)
            {
                var stream = new System.IO.MemoryStream();
                if (mat.Flags.HasFlag(MaterialFlags.Transparent))
                    sub.Bitmap.ToDds(0).WriteToStream(stream, System.Drawing.Imaging.ImageFormat.Png, DecompressOptions.Default);
                else sub.Bitmap.ToDds(0).WriteToStream(stream, System.Drawing.Imaging.ImageFormat.Png, DecompressOptions.Bgr24);
                tex = new Helix.TextureModel(stream);
                sceneTextures.Add(key, tex);
            }

            return tex;
        }
        #endregion

        public void Dispose()
        {
            BspHolder?.Dispose();
            SkyHolder?.Dispose();

            if (PaletteHolders != null)
            {
                foreach (var holder in PaletteHolders.Values)
                    holder.Dispose();
            }

            foreach (var tex in sceneTextures.Values)
                tex.CompressedStream?.Dispose();

            sceneTextures.Clear();
        }
    }
}
