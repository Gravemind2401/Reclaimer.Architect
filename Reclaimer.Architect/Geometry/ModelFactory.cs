using System;
using System.Collections.Generic;
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
using Adjutant.Geometry;
using Adjutant.Blam.Common;
using Adjutant.Utilities;
using Reclaimer.Models;
using Reclaimer.Utilities;
using System.Numerics;

namespace Reclaimer.Geometry
{
    public sealed class ModelFactory : IDisposable
    {
        private static readonly string[] directContentTags = new[] { "mode", "mod2", "sbsp" };
        private static readonly string[] compositeTags = new[] { "hlmt", "weap", "vehi", "bipd", "scen", "eqip", "bloc", "mach", "ctrl" };
        private static readonly Helix.Material ErrorMaterial = Helix.DiffuseMaterials.Gold;

        private readonly ConcurrentDictionary<int, Helix.TextureModel> textureCache = new ConcurrentDictionary<int, Helix.TextureModel>();
        private readonly ConcurrentDictionary<int, TemplateCollection> geometryCache = new ConcurrentDictionary<int, TemplateCollection>();
        private readonly ConcurrentDictionary<int, ModelType> modelTypes = new ConcurrentDictionary<int, ModelType>();
        private readonly ConcurrentDictionary<int, ModelConfig> configCache = new ConcurrentDictionary<int, ModelConfig>();

        public static bool IsTagSupported(IIndexItem tag)
        {
            if (!directContentTags.Union(compositeTags).Any(s => tag?.ClassCode.ToLower() == s))
                return false;

            switch (tag.CacheFile.CacheType)
            {
                case CacheType.Halo3Retail:
                case CacheType.MccHalo3:
                case CacheType.MccHalo3U4:
                case CacheType.Halo3ODST:
                case CacheType.MccHalo3ODST:
                case CacheType.HaloReachRetail:
                case CacheType.MccHaloReach:
                case CacheType.MccHaloReachU3:
                case CacheType.Halo4Retail:
                case CacheType.MccHalo4:
                case CacheType.MccHalo2X:
                    return true;
                default: return false;
            }
        }

        public Helix.Material CreateMaterial(IGeometryMaterial mat, out bool isTransparent)
        {
            var diffuseTexture = ReadTexture(mat);
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

        private Helix.TextureModel ReadTexture(IGeometryMaterial mat)
        {
            var sub = mat?.Submaterials.FirstOrDefault(s => s.Usage == MaterialUsage.Diffuse);
            if (string.IsNullOrEmpty(sub?.Bitmap?.Name))
                return null;

            var key = mat.Flags.HasFlag(MaterialFlags.Transparent) ? -sub.Bitmap.Id : sub.Bitmap.Id;

            Helix.TextureModel tex;
            textureCache.TryGetValue(key, out tex);
            if (tex == null)
            {
                try
                {
                    var args = new DdsOutputArgs(mat.Flags.HasFlag(MaterialFlags.Transparent) ? DecompressOptions.Default : DecompressOptions.Bgr24);
                    var stream = new System.IO.MemoryStream();
                    sub.Bitmap.ToDds(0).WriteToStream(stream, System.Drawing.Imaging.ImageFormat.Png, args);
                    tex = new Helix.TextureModel(stream);
                    if (!textureCache.TryAdd(key, tex))
                        stream.Dispose(); //another thread beat us to it
                }
                catch
                {
                    return null;
                }
            }

            return tex;
        }

        public void LoadTag(IIndexItem tag, bool lods)
        {
            if (tag == null)
                return;

            if (!IsTagSupported(tag))
                throw new NotSupportedException();

            if (compositeTags.Any(s => tag.ClassCode.Equals(s, StringComparison.OrdinalIgnoreCase)))
                LoadCompositeTag(tag, lods);
            else LoadRenderModelTag(tag, lods);
        }

        public void LoadGeometry(IRenderGeometry geom, bool lods)
        {
            LoadRenderGeometry(geom, lods);
        }

        private bool LoadRenderModelTag(IIndexItem tag, bool lods)
        {
            if (geometryCache.ContainsKey(tag.Id))
                return true;

            IRenderGeometry geom;
            if (!ContentFactory.TryGetGeometryContent(tag, out geom))
                return false;

            return LoadRenderGeometry(geom, lods);
        }

        private bool LoadRenderGeometry(IRenderGeometry geom, bool lods)
        {
            if (geometryCache.ContainsKey(geom.Id))
                return true;

            try
            {
                var lodCount = lods ? geom.LodCount : 1;
                var lodModels = Enumerable.Repeat(0, lodCount).Select(i => geom.ReadGeometry(i)).ToList();
                var col = new TemplateCollection(this, lodModels);

                if (!geometryCache.TryAdd(geom.Id, col))
                    return true; //looks like another thread is already working on this model

                foreach (var lod in lodModels)
                {
                    //cache all referenced textures
                    var matIndexes = lod.Meshes.SelectMany(m => m.Submeshes)
                        .Select(m => m.MaterialIndex)
                        .Where(i => i >= 0)
                        .Distinct();

                    foreach (var index in matIndexes)
                        ReadTexture(lod.Materials[index]);
                }

                foreach (var lod in lodModels)
                    lod.Dispose();
            }
            catch
            {
                return false;
            }

            return true;
        }

        private bool LoadCompositeTag(IIndexItem tag, bool lods)
        {
            if (modelTypes.ContainsKey(tag.Id))
                return true;

            string defaultVariant;

            //find the corresponding hlmt tag
            var hlmt = FindModelTag(tag, out defaultVariant);
            if (hlmt == null)
                return false;

            var modelType = new ModelType(tag, defaultVariant);
            if (!modelTypes.TryAdd(tag.Id, modelType))
                return true; //looks like another thread is already working on this model

            if (configCache.ContainsKey(hlmt.Id))
                modelType.VariantConfig = configCache[hlmt.Id];
            else
            {
                var config = modelType.VariantConfig = ModelConfig.FromIndexItem(hlmt);
                if (!configCache.TryAdd(hlmt.Id, config))
                    return true; //looks like another thread is already working on this model

                LoadTag(config.RenderModelTag, lods);

                foreach (var att in config.Variants.SelectMany(v => v.Attachments))
                    LoadTag(att.ChildTag, lods);
            }

            return true;
        }

        private IIndexItem FindModelTag(IIndexItem source, out string defaultVariant)
        {
            defaultVariant = null;

            //already a hlmt tag
            if (source.ClassCode.Equals(compositeTags[0], StringComparison.OrdinalIgnoreCase))
                return source;

            switch (source.CacheFile.CacheType)
            {
                case CacheType.Halo3Retail:
                case CacheType.MccHalo3:
                case CacheType.MccHalo3U4:
                case CacheType.Halo3ODST:
                case CacheType.MccHalo3ODST:
                    var h3Meta = source.ReadMetadata<Blam.Halo3.@object>();
                    defaultVariant = h3Meta.DefaultVariant;
                    return h3Meta.Model.Tag;

                case CacheType.HaloReachRetail:
                case CacheType.MccHaloReach:
                case CacheType.MccHaloReachU3:
                    var reachMeta = source.ReadMetadata<Blam.HaloReach.@object>();
                    defaultVariant = reachMeta.DefaultVariant;
                    return reachMeta.Model.Tag;

                case CacheType.Halo4Retail:
                case CacheType.MccHalo4:
                case CacheType.MccHalo2X:
                    var h4Meta = source.ReadMetadata<Blam.Halo4.@object>();
                    defaultVariant = h4Meta.DefaultVariant;
                    return h4Meta.Model.Tag;

                default: return null;
            }
        }

        public ObjectModel3D CreateObjectModel(int id)
        {
            try
            {
                if (modelTypes.ContainsKey(id))
                {
                    var modelType = modelTypes[id];
                    return new ObjectModel3D(this, modelType.VariantConfig, modelType.ObjectTag.FileName(), modelType.DefaultVariant);
                }
            }
            catch { }

            return new ObjectModel3D(this, ModelConfig.Empty, id.ToString(), string.Empty);
        }

        public RenderModel3D CreateRenderModel(int id) => CreateRenderModel(id, 0);

        public RenderModel3D CreateRenderModel(int id, int lod)
        {
            try
            {
                if (geometryCache.ContainsKey(id))
                    return geometryCache[id].BuildElement(lod);
            }
            catch { }

            return RenderModel3D.Error(id.ToString());
        }

        public RenderModel3D CreateModelSection(int id, int lod, int meshIndex, int meshCount) => CreateModelSection(id, lod, meshIndex, meshCount, 1f, Matrix4x4.Identity);

        public RenderModel3D CreateModelSection(int id, int lod, int meshIndex, int meshCount, float scale, Matrix4x4 transform)
        {
            try
            {
                if (geometryCache.ContainsKey(id))
                {
                    var geom = geometryCache[id];
                    return geom.BuildSection(lod, meshIndex, meshCount, scale, transform);
                }
            }
            catch { }

            return RenderModel3D.Error(id.ToString());
        }

        public ModelProperties GetProperties(int id) => GetProperties(id, 0);

        public ModelProperties GetProperties(int id, int lod)
        {
            if (!geometryCache.ContainsKey(id))
                return null;

            return geometryCache[id].LodProperties[lod];
        }

        public void Dispose()
        {
            foreach (var tex in textureCache.Values)
                tex.CompressedStream?.Dispose();

            textureCache.Clear();
        }

        private class ModelType
        {
            public IIndexItem ObjectTag { get; }
            public string DefaultVariant { get; }
            public ModelConfig VariantConfig { get; set; }

            public ModelType(IIndexItem tag, string defaultVariant)
            {
                ObjectTag = tag;
                DefaultVariant = defaultVariant;
            }

            public override string ToString() => ObjectTag.ToString();
        }

        private class TemplateCollection
        {
            private readonly ModelFactory factory;
            private readonly Dictionary<int, MeshTemplate[]> templates = new Dictionary<int, MeshTemplate[]>();
            public IReadOnlyList<ModelProperties> LodProperties { get; }

            public TemplateCollection(ModelFactory factory, IList<IGeometryModel> lods)
            {
                this.factory = factory;
                LodProperties = lods.Select(g => new ModelProperties(g)).ToList();

                for (int i = 0; i < lods.Count; i++)
                {
                    var lod = lods[i];

                    var meshIndexes = lod.Regions
                        .SelectMany(r => r.Permutations)
                        .SelectMany(p => Enumerable.Range(p.MeshIndex, p.MeshCount))
                        .Distinct();

                    var meshes = new MeshTemplate[lod.Meshes.Count];
                    foreach (var index in meshIndexes)
                        meshes[index] = MeshTemplate.FromModel(lod, index);

                    templates.Add(i++, meshes);
                }
            }

            public RenderModel3D BuildElement(int lod)
            {
                var model = LodProperties[lod];

                var regions = new List<RenderModel3D.Region>();
                var instances = new List<RenderModel3D.InstanceGroup>();

                foreach (var region in model.Regions)
                {
                    var instanceMeshIds = new List<int>();
                    var permutations = new List<RenderModel3D.Permutation>();
                    foreach (var permutation in region.Permutations)
                    {
                        //this permutation is an instance that has already been added
                        if (instanceMeshIds.Contains(permutation.MeshIndex))
                            continue;

                        var elements = new List<Helix.GroupElement3D>();
                        for (int i = permutation.MeshIndex; i < permutation.MeshIndex + permutation.MeshCount; i++)
                        {
                            var template = templates[lod][i];

                            if (template.IsEmpty)
                                continue;

                            if (template.IsInstancing)
                                template = template.Copy();

                            elements.Add(template.GenerateModel((mesh, matIndex) =>
                            {
                                if (matIndex < 0 || matIndex >= model.Materials.Count)
                                {
                                    mesh.IsTransparent = false;
                                    mesh.Material = ErrorMaterial;
                                    return;
                                }

                                bool isTransparent;
                                var mat = factory.CreateMaterial(model.Materials[matIndex], out isTransparent);

                                mesh.IsTransparent = isTransparent;
                                mesh.Material = mat;
                            }));

                            if (template.IsInstancing)
                            {
                                instanceMeshIds.Add(i);

                                var allTransforms = region.Permutations
                                    .Where(p => p.MeshIndex == i)
                                    .Select(p => new KeyValuePair<string, SharpDX.Matrix>(p.Name, GetTransform(p.TransformScale, p.Transform).ToMatrix()));

                                instances.Add(new RenderModel3D.InstanceGroup(region.Name, elements[0], (InstancedMeshTemplate)template, allTransforms));

                                //can only have one mesh if instancing
                                break;
                            }
                        }

                        //this permutation was found to be an instance
                        if (instanceMeshIds.Contains(permutation.MeshIndex))
                            continue;

                        Helix.GroupElement3D permutationRoot;
                        if (elements.Count == 1)
                            permutationRoot = elements[0];
                        else
                        {
                            permutationRoot = new Helix.GroupModel3D();
                            foreach (var e in elements)
                                permutationRoot.Children.Add(e);
                        }

                        permutationRoot.Transform = GetTransform(permutation.TransformScale, permutation.Transform);
                        permutations.Add(new RenderModel3D.Permutation(permutationRoot, permutation.SourceIndex, permutation.Name));
                    }

                    regions.Add(new RenderModel3D.Region(region.SourceIndex, region.Name, permutations));
                }

                return new RenderModel3D(model.Name, regions, instances);
            }

            //builds a single permutation and ignores instancing
            public RenderModel3D BuildSection(int lod, int meshIndex, int meshCount, float scale, Matrix4x4 transform)
            {
                var model = LodProperties[lod];
                var elements = new List<Helix.GroupElement3D>();

                for (int i = meshIndex; i < meshIndex + meshCount; i++)
                {
                    var template = templates[lod][i];

                    if (template.IsEmpty)
                        continue;

                    elements.Add(template.GenerateModelNoInstance((mesh, matIndex) =>
                    {
                        if (matIndex < 0 || matIndex >= model.Materials.Count)
                        {
                            mesh.IsTransparent = false;
                            mesh.Material = ErrorMaterial;
                        }

                        bool isTransparent;
                        var mat = factory.CreateMaterial(model.Materials[matIndex], out isTransparent);

                        mesh.IsTransparent = isTransparent;
                        mesh.Material = mat;
                    }));
                }

                Helix.GroupElement3D permutationRoot;
                if (elements.Count == 1)
                    permutationRoot = elements[0];
                else
                {
                    permutationRoot = new Helix.GroupModel3D();
                    foreach (var e in elements)
                        permutationRoot.Children.Add(e);
                }

                permutationRoot.Transform = GetTransform(scale, transform);

                return RenderModel3D.FromElement(permutationRoot);
            }

            private Media3D.Transform3D GetTransform(float scale, Matrix4x4 transform)
            {
                var tGroup = new Media3D.Transform3DGroup();

                if (scale != 1)
                {
                    var tform = new Media3D.ScaleTransform3D(scale, scale, scale);

                    tform.Freeze();
                    tGroup.Children.Add(tform);
                }

                if (!transform.IsIdentity)
                {
                    var tform = new Media3D.MatrixTransform3D(transform.ToMatrix3D());

                    tform.Freeze();
                    tGroup.Children.Add(tform);
                }

                tGroup.Freeze();
                return tGroup;
            }
        }
    }
}
