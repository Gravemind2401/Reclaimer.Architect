using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reclaimer.Models;

using Helix = HelixToolkit.Wpf.SharpDX;

namespace Reclaimer.Geometry
{
    public class ObjectHolder
    {
        public virtual string Name { get; }

        public ObservableCollection<Helix.Element3D> Elements { get; }

        public ObjectHolder(string name)
            : this()
        {
            Name = name;
        }

        public ObjectHolder()
        {
            Elements = new ObservableCollection<Helix.Element3D>();
        }

        public virtual void Dispose()
        {
            foreach (var e in Elements)
                e.Dispose();

            Elements.Clear();
        }
    }

    public class PaletteHolder : ObjectHolder
    {
        public override string Name => Definition.Name;
        public PaletteDefinition Definition { get; }

        public Helix.GroupModel3D GroupElement { get; internal set; }

        public PaletteHolder(PaletteDefinition definition)
        {
            Definition = definition;
        }

        public override void Dispose()
        {
            base.Dispose();
            GroupElement.Dispose();
            GroupElement = null;
        }
    }
}
