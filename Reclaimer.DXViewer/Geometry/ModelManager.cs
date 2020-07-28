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

        public IReadOnlyList<ModelInstance> Instances => instances;

        public ModelManager(SceneManager scene, IGeometryModel model)
        {
            Scene = scene;
            Model = model;

            for (int i = 0; i < model.Meshes.Count; i++)
                templates.Add(i, MeshTemplate.FromModel(model, i));
        }

        public ModelInstance CreateInstance()
        {
            var element = new Helix.GroupModel3D();
            var modelInstance = new ModelInstance(element);

            foreach (var region in Model.Regions)
            {
                foreach (var perm in region.Permutations)
                {
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

                    var it = template as InstancedMeshTemplate;
                    if (it != null)
                    {
                        var root = it.GenerateModel(Scene, Model);
                        if (it.InstanceCount == 0)
                            element.Children.Add(root);

                        var id = it.AddInstance(tGroup.ToMatrix());
                        modelInstance.AddKey(perm, root);
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

            foreach (var inst in templates.OfType<InstancedMeshTemplate>())
                inst.UpdateInstances();

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

    public class ModelInstance
    {
        private readonly Dictionary<object, Helix.Element3D> tagLookup = new Dictionary<object, Helix.Element3D>();
        private readonly Dictionary<object, Guid> instanceLookup = new Dictionary<object, Guid>();

        public Helix.GroupModel3D Element { get; }

        internal ModelInstance(Helix.GroupModel3D element)
        {
            Element = element;
        }

        internal void AddKey(object key, Helix.Element3D value) => tagLookup.Add(key, value);

        internal void AddInstanceKey(object key, Guid value) => instanceLookup.Add(key, value);

        public Helix.Element3D FindElement(object key) => tagLookup.ValueOrDefault(key);

        public void SetElementVisible(object key, bool visible)
        {
            var instanceId = instanceLookup.ValueOrDefault(key);

            var element = tagLookup.ValueOrDefault(key);
            element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
