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
}
