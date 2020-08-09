using Adjutant.Geometry;
using Reclaimer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using static HelixToolkit.Wpf.SharpDX.Media3DExtension;

using Media = System.Windows.Media;
using Media3D = System.Windows.Media.Media3D;
using Helix = HelixToolkit.Wpf.SharpDX;

namespace Reclaimer.Geometry
{
    public class ModelManager : IDisposable
    {
        private readonly Dictionary<int, MeshTemplate> templates = new Dictionary<int, MeshTemplate>();
        private readonly List<ModelInstance> instances = new List<ModelInstance>();

        public SceneManager Scene { get; }
        public IGeometryModel Model { get; }
        public SharpDX.Matrix Transform { get; }
        public GeometryModelVariant DefaultVariant { get; }

        public IReadOnlyList<ModelInstance> Instances => instances;

        public ModelManager(SceneManager scene, IGeometryModel model)
            : this(scene, model, SharpDX.Matrix.Identity, null) { }

        public ModelManager(SceneManager scene, IGeometryModel model, SharpDX.Matrix transform, GeometryModelVariant variant)
        {
            Scene = scene;
            Model = model;
            Transform = transform;
            DefaultVariant = variant;

            for (int i = 0; i < model.Meshes.Count; i++)
                templates.Add(i, MeshTemplate.FromModel(model, i));
        }

        public void PreloadTextures()
        {
            var matIndexes = Model.Meshes.SelectMany(m => m.Submeshes)
                .Select(m => m.MaterialIndex)
                .Where(i => i >= 0)
                .Distinct();

            foreach (var index in matIndexes)
                Scene.LoadTexture(Model.Materials[index]);
        }

        public ModelInstance GenerateModel() => GenerateModel(DefaultVariant);

        public ModelInstance GenerateModel(GeometryModelVariant variant)
        {
            var element = new Helix.GroupModel3D();
            element.Transform = new Media3D.MatrixTransform3D(Transform.ToMatrix3D());
            var modelInstance = new ModelInstance(element);

            var instanceTemp = new Dictionary<int, MeshTemplate>();

            for (int r = 0; r < Model.Regions.Count; r++)
            {
                var region = Model.Regions[r];
                var vRegionIndex = variant?.RegionLookup[r] ?? byte.MaxValue;

                for (int p = 0; p < region.Permutations.Count; p++)
                {
                    if (vRegionIndex != byte.MaxValue)
                    {
                        var vRegion = variant.Regions[vRegionIndex];
                        if (vRegion.Permutations.Count > 0 && !vRegion.Permutations.Any(vp => vp.BasePermutationIndex == p))
                            continue;
                    }

                    var perm = region.Permutations[p];
                    var template = templates.ValueOrDefault(perm.MeshIndex);
                    if (template == null || template.IsEmpty)
                        continue;

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

                    if (template.IsInstancing)
                    {
                        var inst = instanceTemp.ValueOrDefault(perm.MeshIndex);
                        if (inst == null)
                        {
                            inst = template.Copy();
                            var root = inst.GenerateModel(Scene, Model);
                            element.Children.Add(root);
                            instanceTemp.Add(perm.MeshIndex, inst);
                        }

                        var id = inst.AddInstance(tGroup.ToMatrix());
                        modelInstance.AddInstanceKey(perm, id, inst);
                        continue;
                    }

                    var meshInstance = template.GenerateModel(Scene, Model);

                    Helix.GroupModel3D permGroup;
                    if (tGroup.Children.Count == 0 && perm.MeshCount == 1)
                        permGroup = meshInstance;
                    else
                    {
                        permGroup = new Helix.GroupModel3D();
                        permGroup.Children.Add(meshInstance);

                        if (tGroup.Children.Count > 0)
                            (permGroup.Transform = tGroup).Freeze();

                        for (int i = 1; i < perm.MeshCount; i++)
                        {
                            var nextTemplate = templates.ValueOrDefault(perm.MeshIndex);
                            if (nextTemplate == null || nextTemplate.IsEmpty)
                                continue;

                            permGroup.Children.Add(nextTemplate.GenerateModel(Scene, Model));
                        }
                    }

                    element.Children.Add(permGroup);
                    modelInstance.AddKey(perm, permGroup);
                }
            }

            modelInstance.InitInstances();

            instances.Add(modelInstance);
            return modelInstance;
        }

        public void Dispose()
        {
            foreach (var instance in instances)
            {
                foreach (var element in instance.Element.EnumerateDescendents().Reverse().ToList())
                {
                    (element as Helix.GroupElement3D)?.Children.Clear();
                    element.Dispose();
                }

                instance.Element.Children.Clear();
                instance.Element.Dispose();
            }

            instances.Clear();
        }
    }

    public class ModelInstance : IModelInstance
    {
        private readonly List<MeshTemplate> instanceTemplates = new List<MeshTemplate>();
        private readonly Dictionary<object, Helix.Element3D> tagLookup = new Dictionary<object, Helix.Element3D>();
        private readonly Dictionary<object, Tuple<Guid, MeshTemplate>> instanceLookup = new Dictionary<object, Tuple<Guid, MeshTemplate>>();

        public Helix.GroupModel3D Element { get; }

        internal ModelInstance(Helix.GroupModel3D element)
        {
            Element = element;
            element.Tag = this;
        }

        internal void InitInstances()
        {
            foreach (var inst in instanceTemplates)
                inst.UpdateInstances();
        }

        internal void AddKey(object key, Helix.Element3D value) => tagLookup.Add(key, value);

        internal void AddInstanceKey(object key, Guid value, MeshTemplate template)
        {
            if (!instanceTemplates.Contains(template))
                instanceTemplates.Add(template);

            instanceLookup.Add(key, Tuple.Create(value, template));
        }

        public bool ContainsElement(object key) => tagLookup.ContainsKey(key) || instanceLookup.ContainsKey(key);

        public void SetElementVisible(object key, bool visible)
        {
            var instance = instanceLookup.ValueOrDefault(key);
            if (instance != null)
            {
                instance.Item2.SetInstanceVisible(instance.Item1, visible);
                instance.Item2.UpdateInstances();
                return;
            }

            var element = tagLookup.ValueOrDefault(key);
            element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public SharpDX.BoundingBox GetElementBounds(object key)
        {
            if (tagLookup.ContainsKey(key))
                return tagLookup[key].GetTotalBounds();
            else if (instanceLookup.ContainsKey(key))
            {
                var pair = instanceLookup[key];
                return (pair.Item2 as InstancedMeshTemplate).GetInstanceBounds(pair.Item1);
            }
            else return default(SharpDX.BoundingBox);
        }
    }
}
