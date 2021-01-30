using Adjutant.Blam.Common;
using Reclaimer.Geometry;
using Reclaimer.Models;
using Reclaimer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Helix = HelixToolkit.Wpf.SharpDX;

namespace Reclaimer.Components
{
    public class TerrainComponentManager : ComponentManager
    {
        private const string NullTagName = "<null>";

        private readonly ObjectHolder BspHolder;
        private readonly ObjectHolder SkyHolder;

        public IEnumerable<Helix.Element3D> BspElements => BspHolder.Elements.WhereNotNull();

        public TerrainComponentManager(ScenarioModel scenario)
            : base(scenario)
        {
            BspHolder = new ObjectHolder("sbsp");
            SkyHolder = new ObjectHolder("sky");
        }

        public override Task InitializeResourcesAsync(ModelFactory factory)
        {
            return Task.WhenAll(GetResourceInitializers(factory));
        }

        private IEnumerable<Task> GetResourceInitializers(ModelFactory factory)
        {
            yield return Task.Run(() =>
            {
                for (int i = 0; i < scenario.Bsps.Count; i++)
                    factory.LoadTag(scenario.Bsps[i].Tag, false);
            });

            yield return Task.Run(() =>
            {
                for (int i = 0; i < scenario.Skies.Count; i++)
                    factory.LoadTag(scenario.Skies[i].Tag, false);
            });
        }

        public override void InitializeElements(ModelFactory factory)
        {
            foreach (var bsp in scenario.Bsps)
                BspHolder.Elements.Add(bsp.Tag == null ? null : factory.CreateRenderModel(bsp.Tag.Id));

            foreach (var sky in scenario.Skies)
                SkyHolder.Elements.Add(sky.Tag == null ? null : factory.CreateObjectModel(sky.Tag.Id));

            foreach (var element in GetSceneElements())
                element.IsHitTestVisible = false;
        }

        public override IEnumerable<Helix.Element3D> GetSceneElements()
        {
            return BspHolder.Elements.Concat(SkyHolder.Elements).WhereNotNull();
        }

        public override IEnumerable<TreeItemModel> GetSceneNodes()
        {
            var bspNode = new TreeItemModel { Header = BspHolder.Name, IsChecked = true };
            for (int i = 0; i < BspHolder.Elements.Count; i++)
            {
                var bsp = BspHolder.Elements[i];
                if (bsp == null)
                    continue;

                var tag = scenario.Bsps[i].Tag;
                var permNode = new TreeItemModel { Header = tag?.FileName() ?? NullTagName, IsChecked = true, Tag = bsp };
                bspNode.Items.Add(permNode);
            }

            var skyNode = new TreeItemModel { Header = SkyHolder.Name, IsChecked = true };
            for (int i = 0; i < SkyHolder.Elements.Count; i++)
            {
                var sky = SkyHolder.Elements[i];
                if (sky == null)
                    continue;

                var tag = scenario.Skies[i].Tag;
                var permNode = new TreeItemModel { Header = tag?.FileName() ?? NullTagName, IsChecked = true, Tag = sky };
                skyNode.Items.Add(permNode);
            }

            if (bspNode.HasItems)
                yield return bspNode;

            if (skyNode.HasItems)
                yield return skyNode;
        }

        public override void DisposeSceneElements()
        {
            BspHolder.Dispose();
            SkyHolder.Dispose();
        }
    }
}
