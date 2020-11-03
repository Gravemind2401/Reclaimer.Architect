using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Model.Scene;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Reclaimer.Utilities;

using Media3D = System.Windows.Media.Media3D;
using System.Windows.Data;
using Prism.Mvvm;
using Reclaimer.Geometry;

namespace Reclaimer.Controls
{
    public class RenderModel3D : GroupElement3D
    {
        private readonly List<GroupElement3D> regionGroups;
        private readonly List<GroupElement3D> instanceGroups;

        public IReadOnlyList<Region> Regions { get; }
        public IReadOnlyList<InstanceGroup> InstanceGroups { get; }

        public RenderModel3D(IEnumerable<Region> regions, IEnumerable<InstanceGroup> instances)
        {
            Regions = regions.ToList();
            regionGroups = regions.Select(r => r.Element).ToList();

            foreach (var group in regionGroups)
                Children.Add(group);

            InstanceGroups = instances.ToList();
            instanceGroups = instances.Select(r => r.Element).ToList();

            foreach (var group in instanceGroups)
                Children.Add(group);
        }

        public class Region : BindableBase, IVisibilityToggle
        {
            internal GroupElement3D Element { get; }

            public string Name { get; }
            public IReadOnlyList<Permutation> Permutations { get; }

            private bool isVisible = true;
            public bool IsVisible
            {
                get { return isVisible; }
                set
                {
                    if (SetProperty(ref isVisible, value))
                        Element.IsRendering = isVisible;
                }
            }

            internal Region(string name, IReadOnlyList<Permutation> permutations)
            {
                Name = name;
                Permutations = permutations;

                Element = new GroupModel3D();
                foreach (var perm in permutations)
                    Element.Children.Add(perm.Element);
            }
        }

        public class Permutation : BindableBase, IVisibilityToggle
        {
            internal Element3D Element;

            public string Name { get; }

            private bool isVisible = true;
            public bool IsVisible
            {
                get { return isVisible; }
                set
                {
                    if (SetProperty(ref isVisible, value))
                        Element.IsRendering = isVisible;
                }
            }

            internal Permutation(Element3D element, string name)
            {
                Element = element;
                Name = name;
            }
        }

        public class InstanceGroup : BindableBase, IVisibilityToggle
        {
            internal MeshTemplate Template { get; }
            internal GroupElement3D Element { get; }

            public string Name { get; }
            public IReadOnlyList<Instance> Instances { get; }

            private bool isVisible = true;
            public bool IsVisible
            {
                get { return isVisible; }
                set
                {
                    if (SetProperty(ref isVisible, value))
                        Element.IsRendering = isVisible;
                }
            }

            internal InstanceGroup(string name, GroupElement3D rootElement, InstancedMeshTemplate template, IEnumerable<KeyValuePair<string, SharpDX.Matrix>> instances)
            {
                Name = name;
                Template = template;
                Element = rootElement;

                var temp = new List<Instance>();
                foreach (var inst in instances)
                {
                    var id = Template.AddInstance(inst.Value);
                    temp.Add(new Instance(this, id, name));
                }

                Instances = temp;
                Template.UpdateInstances();
            }
        }

        public class Instance : BindableBase, IVisibilityToggle
        {
            internal InstanceGroup Parent { get; }
            internal Guid Id { get; }

            public string Name { get; }

            private bool isVisible = true;
            public bool IsVisible
            {
                get { return isVisible; }
                set
                {
                    if (SetProperty(ref isVisible, value))
                    {
                        Parent.Template.SetInstanceVisible(Id, isVisible);
                        Parent.Template.UpdateInstances();
                    }
                }
            }

            internal Instance(InstanceGroup parent, Guid id, string name)
            {
                Parent = parent;
                Id = id;
                Name = name;
            }
        }

        public interface IVisibilityToggle
        {
            bool IsVisible { get; set; }
        }
    }
}
