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

        private readonly Dictionary<int, Helix.TextureModel> sceneTextures = new Dictionary<int, Helix.TextureModel>();

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

        public void Dispose()
        {
            foreach (var tex in sceneTextures.Values)
                tex.CompressedStream?.Dispose();

            sceneTextures.Clear();
        }
    }
}
