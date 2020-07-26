using Adjutant.Geometry;
using Reclaimer.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Dds;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Media3D = System.Windows.Media.Media3D;
using Helix = HelixToolkit.Wpf.SharpDX;

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
        private readonly List<MeshManager> meshes = new List<MeshManager>();
        private readonly Dictionary<IGeometryPermutation, Helix.Element3D> elementsByPermutation = new Dictionary<IGeometryPermutation, Helix.Element3D>();

        public SceneManager Scene { get; }
        public IGeometryModel Model { get; }
        public Helix.GroupModel3D Element { get; }
        public bool IsFixed { get; }

        public ModelManager(SceneManager scene, IGeometryModel model)
            : this(scene, model, true) { }

        public ModelManager(SceneManager scene, IGeometryModel model, bool isFixed)
        {
            Scene = scene;
            Model = model;
            IsFixed = isFixed;
            Element = new Helix.GroupModel3D();
            LoadGeometry();
        }

        public Helix.Element3D GetElement(IGeometryPermutation permutation) => elementsByPermutation.ValueOrDefault(permutation);

        private void LoadGeometry()
        {
            foreach (var region in Model.Regions)
            {
                foreach (var perm in region.Permutations)
                {
                    var mesh = GetMesh(perm.MeshIndex);
                    if (mesh == null)
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
                        var tform = new Media3D.MatrixTransform3D(new Media3D.Matrix3D
                        {
                            M11 = perm.Transform.M11,
                            M12 = perm.Transform.M12,
                            M13 = perm.Transform.M13,

                            M21 = perm.Transform.M21,
                            M22 = perm.Transform.M22,
                            M23 = perm.Transform.M23,

                            M31 = perm.Transform.M31,
                            M32 = perm.Transform.M32,
                            M33 = perm.Transform.M33,

                            OffsetX = perm.Transform.M41,
                            OffsetY = perm.Transform.M42,
                            OffsetZ = perm.Transform.M43
                        });

                        tform.Freeze();
                        tGroup.Children.Add(tform);
                    }

                    Helix.GroupModel3D permGroup;
                    if (tGroup.Children.Count == 0 && perm.MeshCount == 1)
                        permGroup = mesh.Element;
                    else
                    {
                        permGroup = new Helix.GroupModel3D();
                        permGroup.Children.Add(mesh.Element);

                        if (tGroup.Children.Count > 0)
                            (permGroup.Transform = tGroup).Freeze();

                        for (int i = 1; i < perm.MeshCount; i++)
                        {
                            var nextMesh = GetMesh(perm.MeshIndex + i);
                            permGroup.Children.Add(nextMesh.Element);
                        }
                    }

                    Element.Children.Add(permGroup);
                    elementsByPermutation.Add(perm, permGroup);
                }
            }
        }

        private MeshManager GetMesh(int index)
        {
            if (index < 0 || index > Model.Meshes.Count)
                return null;

            var mesh = Model.Meshes[index];
            if (mesh.Submeshes.Count == 0)
                return null;

            var manager = new MeshManager(this, mesh);
            meshes.Add(manager);

            //var manager = meshes.ValueOrDefault(index);
            //if (manager == null)
            //{
            //    var mesh = Model.Meshes[index];
            //    if (mesh.Submeshes.Count == 0)
            //        return null;

            //    manager = new MeshManager(this, mesh);
            //    meshes.Add(index, manager);
            //}

            return manager;
        }

        public void Dispose()
        {
            foreach (var manager in meshes)
                manager.Dispose();

            foreach (IDisposable child in Element.Children)
                child.Dispose();

            Element.Children.Clear();
            Element.Dispose();
        }
    }

    public class MeshManager : IDisposable
    {
        public ModelManager Parent { get; }
        public Helix.GroupModel3D Element { get; }
        //public InstanceCollection Instances { get; }

        public MeshManager(ModelManager parent, IGeometryMesh mesh)
        {
            Parent = parent;
            //Instances = new InstanceCollection(parent, mesh);
            //Element = Instances.MeshGroup;
            Element = new Helix.GroupModel3D();
            LoadMesh(mesh);
        }

        private void LoadMesh(IGeometryMesh mesh)
        {
            var texMatrix = SharpDX.Matrix.Identity;
            var boundsMatrix = System.Windows.Media.Media3D.Matrix3D.Identity;

            #region Get Transforms
            if (mesh.BoundsIndex >= 0)
            {
                var bounds = Parent.Model.Bounds[mesh.BoundsIndex.Value];

                texMatrix = new SharpDX.Matrix
                {
                    M11 = bounds.UBounds.Length,
                    M22 = bounds.VBounds.Length,
                    M41 = bounds.UBounds.Min,
                    M42 = bounds.VBounds.Min,
                    M44 = 1
                };

                boundsMatrix = new System.Windows.Media.Media3D.Matrix3D
                {
                    M11 = bounds.XBounds.Length,
                    M22 = bounds.YBounds.Length,
                    M33 = bounds.ZBounds.Length,
                    OffsetX = bounds.XBounds.Min,
                    OffsetY = bounds.YBounds.Min,
                    OffsetZ = bounds.ZBounds.Min
                };
            }
            #endregion

            for (int i = 0; i < mesh.Submeshes.Count; i++)
            {
                var sub = mesh.Submeshes[i];
                var geom = new Helix.MeshGeometry3D();

                var indices = mesh.Indicies.Skip(sub.IndexStart).Take(sub.IndexLength).ToList();
                if (mesh.IndexFormat == IndexFormat.TriangleStrip) indices = indices.Unstrip().ToList();

                var vertStart = indices.Min();
                var vertLength = indices.Max() - vertStart + 1;

                var verts = mesh.Vertices.Skip(vertStart).Take(vertLength);
                var positions = verts.Select(v => new SharpDX.Vector3(v.Position[0].X, v.Position[0].Y, v.Position[0].Z));
                //if (!boundsMatrix.IsIdentity) positions = positions.Select(v => SharpDX.Vector3.TransformCoordinate(v, boundsMatrix));

                var texcoords = verts.Select(v => new SharpDX.Vector2(v.TexCoords[0].X, v.TexCoords[0].Y));
                if (!texMatrix.IsIdentity) texcoords = texcoords.Select(v => SharpDX.Vector2.TransformCoordinate(v, texMatrix));

                geom.Positions = new Helix.Vector3Collection(positions);
                geom.TextureCoordinates = new Helix.Vector2Collection(texcoords);
                geom.Indices = new Helix.IntCollection(indices.Select(j => j - vertStart));

                if (mesh.Vertices[0].Normal.Count > 0)
                {
                    var normals = verts.Select(v => new SharpDX.Vector3(v.Normal[0].X, v.Normal[0].Y, v.Normal[0].Z));
                    geom.Normals = new Helix.Vector3Collection(normals);
                }

                var data = new Helix.MeshGeometryModel3D()
                {
                    Transform = new System.Windows.Media.Media3D.MatrixTransform3D(boundsMatrix),
                    Geometry = geom,
                    Material = Parent.Scene.LoadMaterial(Parent.Model, sub.MaterialIndex),
                    CullMode = SharpDX.Direct3D11.CullMode.None,
                };

                Element.Children.Add(data);
            }
        }

        public void Dispose()
        {
            foreach (IDisposable mesh in Element.Children)
                mesh.Dispose();

            Element.Children.Clear();
            Element.Dispose();
        }
    }

    //public class InstanceCollection : Collection<SharpDX.Matrix>
    //{
    //    private readonly Helix.InstancingMeshGeometryModel3D[] rootInstances;

    //    public Helix.GroupModel3D MeshGroup { get; }

    //    public InstanceCollection(ModelManager parent, IGeometryMesh mesh)
    //    {
    //        rootInstances = new Helix.InstancingMeshGeometryModel3D[mesh.Submeshes.Count];

    //        MeshGroup = new Helix.GroupModel3D();

    //        var texMatrix = SharpDX.Matrix.Identity;
    //        var boundsMatrix = System.Windows.Media.Media3D.Matrix3D.Identity;

    //        #region Get Transforms
    //        if (mesh.BoundsIndex >= 0)
    //        {
    //            var bounds = parent.Model.Bounds[mesh.BoundsIndex.Value];

    //            texMatrix = new SharpDX.Matrix
    //            {
    //                M11 = bounds.UBounds.Length,
    //                M22 = bounds.VBounds.Length,
    //                M41 = bounds.UBounds.Min,
    //                M42 = bounds.VBounds.Min,
    //                M44 = 1
    //            };

    //            boundsMatrix = new System.Windows.Media.Media3D.Matrix3D
    //            {
    //                M11 = bounds.XBounds.Length,
    //                M22 = bounds.YBounds.Length,
    //                M33 = bounds.ZBounds.Length,
    //                OffsetX = bounds.XBounds.Min,
    //                OffsetY = bounds.YBounds.Min,
    //                OffsetZ = bounds.ZBounds.Min
    //            };
    //        }
    //        #endregion

    //        for (int i = 0; i < mesh.Submeshes.Count; i++)
    //        {
    //            var sub = mesh.Submeshes[i];
    //            var geom = new Helix.MeshGeometry3D();

    //            var indices = mesh.Indicies.Skip(sub.IndexStart).Take(sub.IndexLength).ToList();
    //            if (mesh.IndexFormat == IndexFormat.TriangleStrip) indices = indices.Unstrip().ToList();

    //            var vertStart = indices.Min();
    //            var vertLength = indices.Max() - vertStart + 1;

    //            var verts = mesh.Vertices.Skip(vertStart).Take(vertLength);
    //            var positions = verts.Select(v => new SharpDX.Vector3(v.Position[0].X, v.Position[0].Y, v.Position[0].Z));
    //            //if (!boundsMatrix.IsIdentity) positions = positions.Select(v => SharpDX.Vector3.TransformCoordinate(v, boundsMatrix));

    //            var texcoords = verts.Select(v => new SharpDX.Vector2(v.TexCoords[0].X, v.TexCoords[0].Y));
    //            if (!texMatrix.IsIdentity) texcoords = texcoords.Select(v => SharpDX.Vector2.TransformCoordinate(v, texMatrix));

    //            geom.Positions = new Helix.Vector3Collection(positions);
    //            geom.TextureCoordinates = new Helix.Vector2Collection(texcoords);
    //            geom.Indices = new Helix.IntCollection(indices.Select(j => j - vertStart));

    //            if (mesh.Vertices[0].Normal.Count > 0)
    //            {
    //                var normals = verts.Select(v => new SharpDX.Vector3(v.Normal[0].X, v.Normal[0].Y, v.Normal[0].Z));
    //                geom.Normals = new Helix.Vector3Collection(normals);
    //            }

    //            rootInstances[i] = new Helix.InstancingMeshGeometryModel3D()
    //            {
    //                Transform = new System.Windows.Media.Media3D.MatrixTransform3D(boundsMatrix),
    //                Geometry = geom,
    //                Material = parent.Scene.LoadMaterial(parent.Model, sub.MaterialIndex),
    //                CullMode = SharpDX.Direct3D11.CullMode.None,
    //                OctreeManager = new Helix.InstancingModel3DOctreeManager(),
    //                Instances = new List<SharpDX.Matrix>(),
    //                InstanceParamArray = new List<Helix.InstanceParameter>(),
    //            };

    //            MeshGroup.Children.Add(rootInstances[i]);
    //        }
    //    }

    //    #region Overrides
    //    protected override void InsertItem(int index, SharpDX.Matrix item)
    //    {
    //        base.InsertItem(index, item);

    //        foreach (var subMesh in rootInstances)
    //        {
    //            subMesh.Instances.Insert(index, item);
    //            subMesh.InstanceParamArray.Insert(index, new Helix.InstanceParameter
    //            {
    //                 DiffuseColor = new SharpDX.Color4(1,1,1,1),
    //                 EmissiveColor = new SharpDX.Color4(1,1,1,1),
    //                  TexCoordOffset = new SharpDX.Vector2(0,0)
    //            });
    //        }
    //    }

    //    protected override void RemoveItem(int index)
    //    {
    //        base.RemoveItem(index);

    //        foreach (var subMesh in rootInstances)
    //        {
    //            subMesh.Instances.RemoveAt(index);
    //            subMesh.InstanceParamArray.RemoveAt(index);
    //        }
    //    }

    //    protected override void SetItem(int index, SharpDX.Matrix item)
    //    {
    //        base.SetItem(index, item);

    //        foreach (var subMesh in rootInstances)
    //            subMesh.Instances[index] = item;
    //    }

    //    protected override void ClearItems()
    //    {
    //        base.ClearItems();

    //        foreach (var subMesh in rootInstances)
    //        {
    //            subMesh.Instances.Clear();
    //            subMesh.InstanceParamArray.Clear();
    //        }
    //    }
    //    #endregion
    //}
}
