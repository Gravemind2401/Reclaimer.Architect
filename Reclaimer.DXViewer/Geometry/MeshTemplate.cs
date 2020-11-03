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

namespace Reclaimer.Geometry
{
    internal class MeshTemplate
    {
        protected readonly int submeshCount;
        protected readonly int[] matIndex;
        protected readonly Helix.MeshGeometry3D[] submeshes;

        public bool IsEmpty => submeshes.Length < 1;

        public virtual bool IsInstancing => false;

        public static MeshTemplate FromModel(IGeometryModel model, int meshIndex)
        {
            var mesh = model.Meshes[meshIndex];

            if (mesh.IsInstancing)
                return new InstancedMeshTemplate(model, mesh);
            else return new MeshTemplate(model, mesh);
        }

        protected MeshTemplate(MeshTemplate copy)
        {
            submeshCount = copy.submeshCount;
            matIndex = copy.matIndex;
            submeshes = copy.submeshes;
        }

        internal MeshTemplate(IGeometryModel model, IGeometryMesh mesh)
        {
            submeshCount = mesh.Submeshes.Count;
            matIndex = new int[submeshCount];
            submeshes = new Helix.MeshGeometry3D[submeshCount];

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

                submeshes[i] = new Helix.MeshGeometry3D
                {
                    Indices = new Helix.IntCollection(subIndices.Select(j => j - vertStart)),
                    Positions = new Helix.Vector3Collection(subPositions),
                    TextureCoordinates = new Helix.Vector2Collection(subTexcoords)
                };

                if (mesh.Vertices[0].Normal.Count > 0)
                {
                    var subNormals = subVerts.Select(v => new SharpDX.Vector3(v.Normal[0].X, v.Normal[0].Y, v.Normal[0].Z));
                    submeshes[i].Normals = new Helix.Vector3Collection(subNormals);
                }

                submeshes[i].UpdateOctree();
            }
        }

        public virtual Helix.GroupModel3D GenerateModel(SceneManager manager, IGeometryModel model)
        {
            var group = new Helix.GroupModel3D();

            for (int i = 0; i < submeshCount; i++)
            {
                bool isTransparent;
                group.Children.Add(new Helix.MeshGeometryModel3D
                {
                    Geometry = submeshes[i],
                    Material = manager.LoadMaterial(model, matIndex[i], out isTransparent),
                    IsTransparent = isTransparent
                });
            }

            return group;
        }

        public virtual Helix.GroupModel3D GenerateModel(Action<Helix.MeshGeometryModel3D, int> applyMaterial)
        {
            var group = new Helix.GroupModel3D();

            for (int i = 0; i < submeshCount; i++)
            {
                var mesh = new Helix.MeshGeometryModel3D
                {
                    Geometry = submeshes[i],
                };

                applyMaterial(mesh, matIndex[i]);
                group.Children.Add(mesh);
            }

            return group;
        }


        public virtual MeshTemplate Copy() => new MeshTemplate(this);

        public virtual Guid AddInstance(SharpDX.Matrix matrix)
        {
            throw new InvalidOperationException();
        }

        public virtual void SetInstanceVisible(Guid id, bool visible)
        {
            throw new InvalidOperationException();
        }

        public virtual void UpdateInstances()
        {
            throw new InvalidOperationException();
        }
    }

    internal class InstancedMeshTemplate : MeshTemplate
    {
        private static readonly Helix.InstanceParameter defaultInstanceParams = new Helix.InstanceParameter
        {
            DiffuseColor = Media.Colors.White.ToColor4(),
            EmissiveColor = Media.Colors.Black.ToColor4(),
            TexCoordOffset = SharpDX.Vector2.Zero
        };

        private readonly List<InstanceDetails> instances = new List<InstanceDetails>();
        private readonly Helix.InstancingMeshGeometryModel3D[] rootMeshes;

        private Helix.GroupModel3D group;

        public override bool IsInstancing => true;

        internal InstancedMeshTemplate(IGeometryModel model, IGeometryMesh mesh)
            : base(model, mesh)
        {
            rootMeshes = new Helix.InstancingMeshGeometryModel3D[submeshCount];
        }

        protected InstancedMeshTemplate(InstancedMeshTemplate copy)
            : base(copy)
        {
            instances = new List<InstanceDetails>();
            rootMeshes = new Helix.InstancingMeshGeometryModel3D[submeshCount];
        }

        public override Helix.GroupModel3D GenerateModel(SceneManager manager, IGeometryModel model)
        {
            if (group != null)
                return group;

            group = new Helix.GroupModel3D();

            for (int i = 0; i < submeshCount; i++)
            {
                bool isTransparent;
                group.Children.Add(rootMeshes[i] = new Helix.InstancingMeshGeometryModel3D
                {
                    Geometry = submeshes[i],
                    Material = manager.LoadMaterial(model, matIndex[i], out isTransparent),
                    IsTransparent = isTransparent
                });
            }

            return group;
        }

        public override Helix.GroupModel3D GenerateModel(Action<Helix.MeshGeometryModel3D, int> applyMaterial)
        {
            if (group != null)
                return group;

            group = new Helix.GroupModel3D();

            for (int i = 0; i < submeshCount; i++)
            {
                var mesh = rootMeshes[i] = new Helix.InstancingMeshGeometryModel3D
                {
                    Geometry = submeshes[i],
                };

                applyMaterial(mesh, matIndex[i]);
                group.Children.Add(mesh);
            }

            return group;
        }

        public override MeshTemplate Copy() => new InstancedMeshTemplate(this);

        public override Guid AddInstance(SharpDX.Matrix matrix)
        {
            var instance = new InstanceDetails
            {
                Id = Guid.NewGuid(),
                IsVisible = true,
                Transform = matrix,
                InstanceParams = defaultInstanceParams
            };

            instances.Add(instance);

            return instance.Id;
        }

        public override void SetInstanceVisible(Guid id, bool visible)
        {
            var instance = instances.FirstOrDefault(i => i.Id == id);
            instance.IsVisible = visible;
        }

        public override void UpdateInstances()
        {
            //instance properties on the mesh are not observable and
            //need to be assigned a new value for any changes to happen

            var visible = instances.Where(i => i.IsVisible).ToList();

            var idArray = visible.Select(i => i.Id).ToArray(visible.Count);
            var instArray = visible.Select(i => i.Transform).ToArray(visible.Count);
            var paramArray = visible.Select(i => i.InstanceParams).ToArray(visible.Count);

            for (int i = 0; i < submeshCount; i++)
            {
                rootMeshes[i].InstanceIdentifiers = idArray;
                rootMeshes[i].Instances = instArray;
                rootMeshes[i].InstanceParamArray = paramArray;
                rootMeshes[i].InvalidateRender();
            }
        }

        public SharpDX.BoundingBox GetInstanceBounds(Guid id)
        {
            var bounds = group.GetTotalBounds(true);
            var instance = instances.FirstOrDefault(i => i.Id == id);

            var newMin = SharpDX.Vector3.TransformCoordinate(bounds.Minimum, instance.Transform);
            var newMax = SharpDX.Vector3.TransformCoordinate(bounds.Maximum, instance.Transform);
            return new SharpDX.BoundingBox(newMin, newMax);
        }

        private class InstanceDetails
        {
            public Guid Id { get; set; }
            public bool IsVisible { get; set; }
            public SharpDX.Matrix Transform { get; set; }
            public Helix.InstanceParameter InstanceParams { get; set; }
        }
    }
}
