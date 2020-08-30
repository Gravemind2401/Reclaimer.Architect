using Reclaimer.Models;
using Reclaimer.Plugins.MetaViewer.Halo3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Reclaimer.Controls
{
    /// <summary>
    /// Interaction logic for PropertyView.xaml
    /// </summary>
    public partial class PropertyView : IScenarioPropertyView
    {
        private ScenarioModel scenario;
        private MetaContext context;

        public TabModel TabModel { get; }
        public ObjectPlacement CurrentItem { get; private set; }

        public PropertyView()
        {
            InitializeComponent();
            TabModel = new TabModel(this, Studio.Controls.TabItemType.Tool) { Header = "Properties" };
        }

        public void ClearScenario()
        {
            ClearProperties();
            scenario = null;
        }

        public void SetScenario(ScenarioModel scenario)
        {
            this.scenario = scenario;
            context = new MetaContext(scenario.ScenarioTag.CacheFile, scenario.ScenarioTag, scenario.Transaction);
        }

        public void ClearProperties()
        {
            metaViewer.Metadata.Clear();
            metaViewer.Visibility = Visibility.Hidden;
        }

        public void SetValue(string id, object value) => metaViewer.SetValue(id, value);

        public void ShowProperties(NodeType nodeType, int itemIndex)
        {
            metaViewer.Metadata.Clear();
            metaViewer.Visibility = Visibility.Hidden;

            var paletteKey = PaletteType.FromNodeType(nodeType);
            if (paletteKey != null && itemIndex >= 0)
            {
                var palette = scenario.Palettes[paletteKey];
                var baseAddress = palette.PlacementBlockRef.TagBlock.Pointer.Address
                    + itemIndex * palette.PlacementBlockRef.BlockSize;

                metaViewer.LoadMetadata(context, palette.PlacementsNode, baseAddress);
                CurrentItem = palette.Placements[itemIndex];
            }
            else
            {
                CurrentItem = null;

                switch (nodeType)
                {
                    case NodeType.Mission:
                        metaViewer.LoadMetadata(context, scenario.Sections["mission"].Node, scenario.ScenarioTag.MetaPointer.Address);
                        break;

                    default:
                        return;
                }
            }

            metaViewer.Visibility = Visibility.Visible;
        }
    }
}
