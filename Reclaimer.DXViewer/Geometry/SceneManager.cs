using Adjutant.Geometry;
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
using System.Windows;

namespace Reclaimer.Geometry
{
    public class SceneManager : IDisposable
    {
        private static readonly Helix.Material ErrorMaterial = Helix.DiffuseMaterials.Gold;

        private readonly Dictionary<string, Helix.TextureModel> sceneTextures = new Dictionary<string, Helix.TextureModel>();

        public Helix.Material LoadMaterial(IGeometryModel model, int matIndex)
        {
            if (matIndex < 0 || matIndex >= model.Materials.Count)
                return ErrorMaterial;

            return LoadMaterial(model.Materials[matIndex]);
        }

        public Helix.Material LoadMaterial(IGeometryMaterial mat)
        {
            var diffuseTexture = GetTexture(mat, MaterialUsage.Diffuse);
            if (diffuseTexture == null)
                return ErrorMaterial;

            var material = new Helix.DiffuseMaterial
            {
                DiffuseMap = diffuseTexture
            };

            material.Freeze();
            return material;
        }

        private Helix.TextureModel GetTexture(IGeometryMaterial mat, MaterialUsage usage)
        {
            var sub = mat.Submaterials.FirstOrDefault(s => s.Usage == MaterialUsage.Diffuse);
            if (string.IsNullOrEmpty(sub?.Bitmap?.Name))
                return null;

            var key = sub.Bitmap.Name;
            var tex = sceneTextures.ValueOrDefault(key);
            if (tex == null)
            {
                var stream = new System.IO.MemoryStream();
                sub.Bitmap.ToDds(0).WriteToStream(stream, System.Drawing.Imaging.ImageFormat.Png, DecompressOptions.Bgr24);
                tex = new Helix.TextureModel(stream);
                sceneTextures.Add(key, tex);
            }

            return tex;
        }

        public void Dispose()
        {
            foreach (var tex in sceneTextures.Values)
                tex.CompressedStream?.Dispose();

            sceneTextures.Clear();
        }
    }
}
