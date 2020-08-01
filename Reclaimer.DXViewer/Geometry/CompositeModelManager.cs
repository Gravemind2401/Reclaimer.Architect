using Adjutant.Geometry;
using Reclaimer.Utilities;
using System;
using System.Collections.Generic;
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
    public class CompositeModelManager : IDisposable
    {
        private readonly ModelManager baseManager;
        private readonly Dictionary<string, List<ModelManager>> managersByVariant = new Dictionary<string, List<ModelManager>>();
        private readonly List<CompositeModelInstance> instances = new List<CompositeModelInstance>();

        public SceneManager Scene { get; }
        public CompositeGeometryModel Model { get; }

        public CompositeModelManager(SceneManager scene, CompositeGeometryModel model)
        {
            Scene = scene;
            Model = model;

            baseManager = new ModelManager(Scene, Model.BaseModel);

            foreach (var variant in model.Variants)
            {
                managersByVariant.Add(variant.Name, new List<ModelManager>());

                foreach (var att in variant.Attachments)
                    AddDefaults(att, managersByVariant[variant.Name]);
            }
        }

        private void AddDefaults(GeometryModelAttachment attached, List<ModelManager> collection)
        {
            var parentMarker = Model.BaseModel.MarkerGroups.FirstOrDefault(g => g.Name == attached.ParentMarker)?.Markers.First();
            var childMarker = attached.ChildModel.BaseModel.MarkerGroups.FirstOrDefault(g => g.Name == attached.ChildMarker)?.Markers.First();

            var mat = SharpDX.Matrix.Identity;
            if (parentMarker != null)
            {
                mat *= SharpDX.Matrix.RotationQuaternion(parentMarker.Rotation.ToQuaternion());
                mat *= SharpDX.Matrix.Translation(parentMarker.Position.ToVector3());
            }
            if (childMarker != null)
            {
                var mat2 = SharpDX.Matrix.RotationQuaternion(childMarker.Rotation.ToQuaternion());
                mat2.TranslationVector = childMarker.Position.ToVector3();
                mat2.Invert();
                mat *= mat2;
            }

            var defaultVariant = attached.ChildModel.Variants.FirstOrDefault(v => v.Name == attached.ChildVariant) ?? attached.ChildModel.Variants.FirstOrDefault();
            collection.Add(new ModelManager(Scene, attached.ChildModel.BaseModel, mat, defaultVariant));

            if (defaultVariant != null)
            {
                foreach (var att in defaultVariant.Attachments)
                    AddDefaults(att, collection);
            }
        }

        public CompositeModelInstance GenerateModel()
        {
            var lookup = new Dictionary<string, List<ModelInstance>>();

            if (!Model.Variants.Any())
                lookup.Add(string.Empty, new List<ModelInstance> { baseManager.GenerateModel() });

            foreach (var variant in Model.Variants)
            {
                lookup.Add(variant.Name, new List<ModelInstance>());

                lookup[variant.Name].Add(baseManager.GenerateModel(variant));
                foreach (var man in managersByVariant[variant.Name])
                    lookup[variant.Name].Add(man.GenerateModel());
            }

            var instance = new CompositeModelInstance(lookup);
            instances.Add(instance);
            return instance;
        }

        public void Dispose()
        {
            foreach (var manager in managersByVariant.SelectMany(p => p.Value))
                manager.Dispose();

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

    public class CompositeModelInstance
    {
        private readonly Dictionary<string, List<ModelInstance>> lookup = new Dictionary<string, List<ModelInstance>>();

        public Helix.GroupModel3D Element { get; }

        internal CompositeModelInstance(Dictionary<string, List<ModelInstance>> variants)
        {
            lookup = variants;

            Element = new Helix.GroupModel3D();

            foreach (var child in lookup.Values.SelectMany(v => v))
                Element.Children.Add(child.Element);

            SetVariant(lookup.Keys.First());
        }

        public void SetVariant(string variant)
        {
            foreach (var pair in lookup)
            {
                foreach (var inst in pair.Value)
                    inst.Element.Visibility = pair.Key == variant ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
