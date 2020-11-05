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

namespace Reclaimer.Geometry
{
    public class ModelFactory
    {
        private static readonly string[] compositeTags = new[] { "hlmt", "weap", "vehi", "bipd", "scen", "eqip", "bloc" };
        private static readonly Helix.Material ErrorMaterial = Helix.DiffuseMaterials.Gold;

        private readonly ConcurrentDictionary<int, Helix.TextureModel> textureCache = new ConcurrentDictionary<int, Helix.TextureModel>();
        private readonly Dictionary<int, TemplateCollection> geometryCache = new Dictionary<int, TemplateCollection>();
        private readonly Dictionary<int, ModelType> modelTypes = new Dictionary<int, ModelType>();
        private readonly Dictionary<int, ModelConfig> configCache = new Dictionary<int, ModelConfig>();

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
                var stream = new System.IO.MemoryStream();
                if (mat.Flags.HasFlag(MaterialFlags.Transparent))
                    sub.Bitmap.ToDds(0).WriteToStream(stream, System.Drawing.Imaging.ImageFormat.Png, DecompressOptions.Default);
                else sub.Bitmap.ToDds(0).WriteToStream(stream, System.Drawing.Imaging.ImageFormat.Png, DecompressOptions.Bgr24);
                tex = new Helix.TextureModel(stream);
                if (!textureCache.TryAdd(key, tex))
                    stream.Dispose(); //another thread beat us to it
            }

            return tex;
        }

        public void LoadTag(IIndexItem tag, bool lods)
        {
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

            var lodCount = lods ? geom.LodCount : 1;
            var lodModels = Enumerable.Repeat(0, lodCount).Select(i => geom.ReadGeometry(i)).ToList();
            var col = new TemplateCollection(this, lodModels);

            geometryCache.Add(geom.Id, col);

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
            modelTypes.Add(tag.Id, modelType);

            if (configCache.ContainsKey(hlmt.Id))
                modelType.VariantConfig = configCache[hlmt.Id];
            else
            {
                var config = modelType.VariantConfig = ModelConfig.FromIndexItem(hlmt);
                configCache.Add(hlmt.Id, config);

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
                    var meta = source.ReadMetadata<Blam.Halo3.@object>();
                    defaultVariant = meta.DefaultVariant;
                    return meta.Model.Tag;

                default: return null;
            }
        }

        public ObjectModel3D CreateObjectModel(int id)
        {
            if (!modelTypes.ContainsKey(id))
                return null;

            var modelType = modelTypes[id];
            return new ObjectModel3D(this, modelType.VariantConfig, modelType.ObjectTag.FileName(), modelType.DefaultVariant);
        }

        public RenderModel3D CreateRenderModel(int id) => CreateRenderModel(id, 0);

        public RenderModel3D CreateRenderModel(int id, int lod)
        {
            if (!geometryCache.ContainsKey(id))
                return null;

            return geometryCache[id].BuildElement(lod);
        }

        public ModelProperties GetProperties(int id) => GetProperties(id, 0);

        public ModelProperties GetProperties(int id, int lod)
        {
            if (!geometryCache.ContainsKey(id))
                return null;

            return geometryCache[id].LodProperties[lod];
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
                                    .Select(p => new KeyValuePair<string, SharpDX.Matrix>(p.Name, GetTransform(p).ToMatrix()));

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

                        permutationRoot.Transform = GetTransform(permutation);
                        permutations.Add(new RenderModel3D.Permutation(permutationRoot, permutation.Name));
                    }

                    regions.Add(new RenderModel3D.Region(region.Name, permutations));
                }

                return new RenderModel3D(model.Name, regions, instances);
            }

            private Media3D.Transform3D GetTransform(IGeometryPermutation perm)
            {
                var tGroup = new Media3D.Transform3DGroup();

                if (perm.TransformScale != 1)
                {
                    var tform = new Media3D.ScaleTransform3D(perm.TransformScale, perm.TransformScale, perm.TransformScale);

                    tform.Freeze();
                    tGroup.Children.Add(tform);
                }

                if (!perm.Transform.IsIdentity)
                {
                    var tform = new Media3D.MatrixTransform3D(perm.Transform.ToMatrix3D());

                    tform.Freeze();
                    tGroup.Children.Add(tform);
                }

                tGroup.Freeze();
                return tGroup;
            }
        }
    }
}
