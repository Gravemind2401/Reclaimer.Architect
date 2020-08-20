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
        public ObservableCollection<ModelManager> Managers { get; }
        public ObservableCollection<ModelInstance> Instances { get; }

        public ObjectHolder()
        {
            Managers = new ObservableCollection<ModelManager>();
            Instances = new ObservableCollection<ModelInstance>();
        }

        public virtual void Dispose()
        {
            foreach (var man in Managers)
            {
                man?.Model.Dispose();
                man?.Dispose();
            }

            Managers.Clear();
            Instances.Clear();
        }
    }

    public class CompositeObjectHolder
    {
        public ObservableCollection<CompositeModelManager> Managers { get; }
        public ObservableCollection<CompositeModelInstance> Instances { get; }

        public CompositeObjectHolder()
        {
            Managers = new ObservableCollection<CompositeModelManager>();
            Instances = new ObservableCollection<CompositeModelInstance>();
        }

        public virtual void Dispose()
        {
            foreach (var man in Managers)
            {
                man?.Model.Dispose();
                man?.Dispose();
            }

            Managers.Clear();
            Instances.Clear();
        }
    }

    public class PaletteHolder : CompositeObjectHolder
    {
        public string Name => Definition.Name;
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
