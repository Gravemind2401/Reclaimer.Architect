﻿using HelixToolkit.Wpf.SharpDX;
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
using Reclaimer.Models;

namespace Reclaimer.Controls
{
    public sealed class RenderModel3D : GroupElement3D, IMeshNode
    {
        private readonly List<GroupElement3D> regionGroups;
        private readonly List<GroupElement3D> instanceGroups;

        public string ModelName { get; }
        public IReadOnlyList<Region> Regions { get; }
        public IReadOnlyList<InstanceGroup> InstanceGroups { get; }

        public RenderModel3D(string name, IEnumerable<Region> regions, IEnumerable<InstanceGroup> instances)
        {
            ModelName = name;
            Regions = regions.ToList();
            regionGroups = regions.Select(r => r.Element).ToList();

            foreach (var group in regionGroups)
                Children.Add(group);

            InstanceGroups = instances.ToList();
            instanceGroups = instances.Select(r => r.Element).ToList();

            foreach (var group in instanceGroups)
                Children.Add(group);
        }

        public void ShowAll()
        {
            foreach (var region in Regions)
            {
                foreach (var perm in region.Permutations)
                    perm.IsVisible = true;

                region.IsVisible = true;
            }

            foreach (var group in InstanceGroups)
            {
                foreach (var inst in group.Instances)
                    inst.IsVisible = true;

                group.IsVisible = true;
            }
        }

        public void ApplyVariant(VariantConfig variant)
        {
            for (int i = 0; i < Regions.Count; i++)
            {
                var region = Regions[i];
                var vRegionIndex = variant?.RegionLookup[i] ?? byte.MaxValue;

                for (int j = 0; j < region.Permutations.Count; j++)
                {
                    var perm = region.Permutations[j];

                    if (vRegionIndex != byte.MaxValue)
                    {
                        var vRegion = variant.Regions[vRegionIndex];
                        if (vRegion.Permutations.Count > 0 && !vRegion.Permutations.Any(vp => vp.BasePermutationIndex == j))
                        {
                            perm.IsVisible = false;
                            continue;
                        }
                    }

                    perm.IsVisible = true;
                }

                region.IsVisible = region.Permutations.Any(p => p.IsVisible);
            }
        }

        #region IMeshNode

        string IMeshNode.Name => ModelName;

        bool IMeshNode.IsVisible
        {
            get { return IsRendering; }
            set { IsRendering = value; }
        }

        BoundingBox IMeshNode.GetNodeBounds()
        {
            return regionGroups.OfType<IMeshNode>()
                .Union(InstanceGroups.OfType<IMeshNode>())
                .Select(n => n.GetNodeBounds())
                .GetTotalBounds();
        }

        #endregion

        public class Region : BindableBase, IMeshNode
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

            public BoundingBox GetNodeBounds()
            {
                return Element.GetTotalBounds();
            }
        }

        public class Permutation : BindableBase, IMeshNode
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

            public BoundingBox GetNodeBounds()
            {
                return Element.GetTotalBounds();
            }
        }

        public class InstanceGroup : BindableBase, IMeshNode
        {
            internal InstancedMeshTemplate Template { get; }
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

            public BoundingBox GetNodeBounds()
            {
                return Instances.Select(i => i.GetNodeBounds()).GetTotalBounds();
            }
        }

        public class Instance : BindableBase, IMeshNode
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

            public BoundingBox GetNodeBounds()
            {
                return Parent.Template.GetInstanceBounds(Id);
            }
        }
    }
}
