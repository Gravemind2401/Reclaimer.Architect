using Reclaimer.Models;
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
        public TabModel TabModel { get; }

        private ScenarioModel scenario;

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
        }

        public void ClearProperties()
        {
            metaViewer.Metadata.Clear();
            metaViewer.Visibility = Visibility.Hidden;
        }

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

                metaViewer.LoadMetadata(scenario.ScenarioTag, palette.PlacementsNode, baseAddress);
            }
            else
            {
                switch (nodeType)
                {
                    case NodeType.Mission:
                        metaViewer.LoadMetadata(scenario.ScenarioTag, scenario.Sections["mission"].Node, scenario.ScenarioTag.MetaPointer.Address);
                        break;

                    default:
                        return;
                }
            }

            metaViewer.Visibility = Visibility.Visible;
        }
    }
}
