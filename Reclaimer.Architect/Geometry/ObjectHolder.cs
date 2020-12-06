using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reclaimer.Models;

using Helix = HelixToolkit.Wpf.SharpDX;
using Reclaimer.Utilities;

namespace Reclaimer.Geometry
{
    public class ObjectHolder
    {
        public virtual string Name { get; }

        public List<Helix.Element3D> Elements { get; }

        public ObjectHolder(string name)
            : this()
        {
            Name = name;
        }

        public ObjectHolder()
        {
            Elements = new List<Helix.Element3D>();
        }

        public virtual void Dispose()
        {
            foreach (var e in Elements)
                e?.Dispose();

            Elements.Clear();
        }
    }

    public class PaletteHolder : ObjectHolder
    {
        public override string Name => Definition.Name;
        public PaletteDefinition Definition { get; }

        public List<TreeItemModel> TreeItems { get; }

        public Helix.GroupModel3D GroupElement { get; internal set; }

        public PaletteHolder(PaletteDefinition definition)
        {
            TreeItems = new List<TreeItemModel>();
            Definition = definition;
        }

        public ObjectInfo GetInfoForIndex(int index) => new ObjectInfo(this, index);

        public void SetCapacity(int newSize)
        {
            Elements.Resize(newSize);
            TreeItems.Resize(newSize);
        }

        public override void Dispose()
        {
            base.Dispose();
            GroupElement.Dispose();
            GroupElement = null;
        }

        public struct ObjectInfo
        {
            private readonly PaletteHolder holder;
            private readonly int index;

            public Helix.Element3D Element
            {
                get { return holder.Elements[index]; }
                set { holder.Elements[index] = value; }
            }

            public ObjectPlacement Placement
            {
                get { return holder.Definition.Placements[index]; }
                set { holder.Definition.Placements[index] = value; }
            }

            public TreeItemModel TreeItem
            {
                get { return holder.TreeItems[index]; }
                set { holder.TreeItems[index] = value; }
            }

            public ObjectInfo(PaletteHolder holder, int index)
            {
                this.holder = holder;
                this.index = index;
            }
        }
    }

    public class InstanceHolder : ObjectHolder
    {
        private readonly int sectionIndex;

        public override string Name => $"Instances {sectionIndex:D3}";
        public List<TreeItemModel> TreeItems { get; }
        public List<InstancePlacement> Placements { get; }

        public Helix.GroupModel3D GroupElement { get; internal set; }

        public InstanceHolder(int sectionIndex, ModelProperties props, IList<IBspGeometryInstanceBlock> instanceData)
        {
            this.sectionIndex = sectionIndex;
            TreeItems = new List<TreeItemModel>();
            Placements = new List<InstancePlacement>();

            var perms = props.Regions.SelectMany(r => r.Permutations)
                .Where(p => p.MeshIndex == sectionIndex);

            foreach (var p in perms)
            {
                var source = instanceData[p.SourceIndex];
                Placements.Add(new InstancePlacement(p.SourceIndex)
                {
                    Name = p.Name,
                    MeshIndex = p.MeshIndex,
                    TransformScale = p.TransformScale,
                    Transform = p.Transform,
                    SphereX = source.BoundingSpherePosition.X,
                    SphereY = source.BoundingSpherePosition.Y,
                    SphereZ = source.BoundingSpherePosition.Z,
                    SphereRadius = source.BoundingSphereRadius
                });
            }

            SetCapacity(Placements.Count);
        }

        public void SetCapacity(int newSize)
        {
            Elements.Resize(newSize);
            TreeItems.Resize(newSize);
            Placements.Resize(newSize);
        }

        public override void Dispose()
        {
            base.Dispose();
            GroupElement.Dispose();
            GroupElement = null;
        }
    }
}
