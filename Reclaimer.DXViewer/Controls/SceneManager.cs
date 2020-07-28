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

namespace Reclaimer.Controls
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

                    var it = template as InstanceTemplate;
                    if (it != null)
                    {
                        var root = it.CreateInstance(Scene, Model);
                        if (it.InstanceCount == 0)
                            element.Children.Add(root);

                        var id = it.AddInstance(tGroup.ToMatrix());
                        modelInstance.AddKey(perm, root);
                        continue;
                    }

                    var meshInstance = template.CreateInstance(Scene, Model);

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

                            permGroup.Children.Add(nextTemplate.CreateInstance(Scene, Model));
                        }
                    }

                    element.Children.Add(permGroup);
                    modelInstance.AddKey(perm, permGroup);
                }
            }

            foreach (var inst in templates.OfType<InstanceTemplate>())
                inst.RefreshInstances();

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

        private class MeshTemplate
        {
            protected readonly int submeshCount;
            protected readonly int[] matIndex;
            protected readonly Helix.IntCollection[] indices;
            protected readonly Helix.Vector3Collection[] positions;
            protected readonly Helix.Vector3Collection[] normals;
            protected readonly Helix.Vector2Collection[] texcoords;

            public bool IsEmpty => submeshCount < 1;

            public static MeshTemplate FromModel(IGeometryModel model, int meshIndex)
            {
                var mesh = model.Meshes[meshIndex];

                if (mesh.IsInstancing)
                    return new InstanceTemplate(model, mesh);
                else return new MeshTemplate(model, mesh);
            }

            protected MeshTemplate(MeshTemplate copy)
            {
                submeshCount = copy.submeshCount;
                matIndex = copy.matIndex;
                indices = copy.indices;
                positions = copy.positions;
                normals = copy.normals;
                texcoords = copy.texcoords;
            }

            public MeshTemplate(IGeometryModel model, IGeometryMesh mesh)
            {
                submeshCount = mesh.Submeshes.Count;
                matIndex = new int[submeshCount];
                indices = new Helix.IntCollection[submeshCount];
                positions = new Helix.Vector3Collection[submeshCount];
                normals = new Helix.Vector3Collection[submeshCount];
                texcoords = new Helix.Vector2Collection[submeshCount];

                var texMatrix = SharpDX.Matrix.Identity;
                var boundsMatrix = SharpDX.Matrix.Identity;

                if (mesh.BoundsIndex >= 0)
                {
                    var bounds = model.Bounds[mesh.BoundsIndex.Value];
                    boundsMatrix = bounds.ToMatrix3();
                    texMatrix = bounds.ToMatrix2();
                }

                for (int i = 0; i < submeshCount; i++)
                {
                    var sub = mesh.Submeshes[i];
                    matIndex[i] = sub.MaterialIndex;

                    var subIndices = mesh.Indicies.Skip(sub.IndexStart).Take(sub.IndexLength).ToList();
                    if (mesh.IndexFormat == IndexFormat.TriangleStrip) subIndices = subIndices.Unstrip().ToList();

                    var vertStart = subIndices.Min();
                    var vertLength = subIndices.Max() - vertStart + 1;
                    var subVerts = mesh.Vertices.Skip(vertStart).Take(vertLength);

                    IEnumerable<SharpDX.Vector3> subPositions;
                    if (boundsMatrix.IsIdentity)
                        subPositions = subVerts.Select(v => v.Position[0].ToVector3());
                    else
                        subPositions = subVerts.Select(v => SharpDX.Vector3.TransformCoordinate(v.Position[0].ToVector3(), boundsMatrix));

                    IEnumerable<SharpDX.Vector2> subTexcoords;
                    if (texMatrix.IsIdentity)
                        subTexcoords = subVerts.Select(v => v.TexCoords[0].ToVector2());
                    else
                        subTexcoords = subVerts.Select(v => SharpDX.Vector2.TransformCoordinate(v.TexCoords[0].ToVector2(), texMatrix));

                    indices[i] = new Helix.IntCollection(subIndices.Select(j => j - vertStart));
                    positions[i] = new Helix.Vector3Collection(subPositions);
                    texcoords[i] = new Helix.Vector2Collection(subTexcoords);

                    if (mesh.Vertices[0].Normal.Count > 0)
                    {
                        var subNormals = subVerts.Select(v => new SharpDX.Vector3(v.Normal[0].X, v.Normal[0].Y, v.Normal[0].Z));
                        normals[i] = new Helix.Vector3Collection(subNormals);
                    }
                }
            }

            public virtual Helix.GroupModel3D CreateInstance(SceneManager manager, IGeometryModel model)
            {
                var group = new Helix.GroupModel3D();

                for (int i = 0; i < submeshCount; i++)
                {
                    var geom = new Helix.MeshGeometry3D
                    {
                        Indices = indices[i],
                        Positions = positions[i],
                        Normals = normals[i],
                        TextureCoordinates = texcoords[i]
                    };

                    group.Children.Add(new Helix.MeshGeometryModel3D
                    {
                        Geometry = geom,
                        Material = manager.LoadMaterial(model, matIndex[i])
                    });
                }

                return group;
            }
        }

        private class InstanceTemplate : MeshTemplate
        {
            private static readonly Helix.InstancingModel3DOctreeManager octreeManager = new Helix.InstancingModel3DOctreeManager();

            private readonly Helix.InstanceParameter defaultInstanceParam = new Helix.InstanceParameter
            {
                DiffuseColor = Media.Colors.White.ToColor4(),
                EmissiveColor = Media.Colors.Black.ToColor4(),
                TexCoordOffset = SharpDX.Vector2.Zero
            };

            private readonly List<Guid> identifiers = new List<Guid>();
            private readonly List<SharpDX.Matrix> instances = new List<SharpDX.Matrix>();
            private readonly List<Helix.InstanceParameter> instanceParams = new List<Helix.InstanceParameter>();

            private readonly Helix.InstancingMeshGeometryModel3D[] rootMeshes;

            private Helix.GroupModel3D group;

            public InstanceTemplate(IGeometryModel model, IGeometryMesh mesh)
                : base(model, mesh)
            {
                rootMeshes = new Helix.InstancingMeshGeometryModel3D[submeshCount];
            }

            private InstanceTemplate(InstanceTemplate copy)
                : base(copy)
            {
                identifiers = new List<Guid>();
                instances = new List<SharpDX.Matrix>();
                instanceParams = new List<Helix.InstanceParameter>();
            }

            public override Helix.GroupModel3D CreateInstance(SceneManager manager, IGeometryModel model)
            {
                if (group != null)
                    return group;

                group = new Helix.GroupModel3D();

                for (int i = 0; i < submeshCount; i++)
                {
                    var geom = new Helix.MeshGeometry3D
                    {
                        Indices = indices[i],
                        Positions = positions[i],
                        Normals = normals[i],
                        TextureCoordinates = texcoords[i]
                    };

                    group.Children.Add(rootMeshes[i] = new Helix.InstancingMeshGeometryModel3D
                    {
                        Geometry = geom,
                        Material = manager.LoadMaterial(model, matIndex[i]),
                        OctreeManager = new Helix.InstancingModel3DOctreeManager()
                    });
                }

                return group;
            }

            public int InstanceCount => instances.Count;

            public InstanceTemplate Copy() => new InstanceTemplate(this);

            public Guid AddInstance(SharpDX.Matrix matrix)
            {
                var id = Guid.NewGuid();

                identifiers.Add(id);
                instances.Add(matrix);
                instanceParams.Add(defaultInstanceParam);

                return id;
            }

            public void RemoveInstance(Guid id)
            {
                var index = identifiers.IndexOf(id);

                identifiers.RemoveAt(index);
                instances.RemoveAt(index);
                instanceParams.RemoveAt(index);
            }

            public void RefreshInstances()
            {
                var idArray = identifiers.ToArray();
                var instArray = instances.ToArray();
                var paramArray = instanceParams.ToArray();

                for (int i = 0; i < submeshCount; i++)
                {
                    rootMeshes[i].InstanceIdentifiers = idArray;
                    rootMeshes[i].Instances = instArray;
                    rootMeshes[i].InstanceParamArray = paramArray;
                    rootMeshes[i].InvalidateRender();
                }
            }
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
