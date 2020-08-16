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
    public partial class PropertyView : UserControl
    {
        public TabModel TabModel { get; }

        private ScenarioModel scenario;
        public ScenarioModel Scenario
        {
            get { return scenario; }
            set
            {
                if (value != scenario)
                {
                    OnScenarioUnset();
                    scenario = value;
                    OnScenarioSet();
                }
            }
        }

        public PropertyView()
        {
            InitializeComponent();
            TabModel = new TabModel(this, Studio.Controls.TabItemType.Tool);
        }

        private void OnScenarioUnset()
        {
            metaViewer.Metadata.Clear();
            metaViewer.Visibility = Visibility.Hidden;

            if (scenario != null)
            {
                scenario.SelectedNodeChanged -= Scenario_SelectionChanged;
                scenario.SelectedItemChanged -= Scenario_SelectionChanged;
            }
        }

        private void OnScenarioSet()
        {
            if (scenario != null)
            {
                scenario.SelectedNodeChanged += Scenario_SelectionChanged;
                scenario.SelectedItemChanged += Scenario_SelectionChanged;
                LoadNodeData();
            }
        }

        private void Scenario_SelectionChanged(object sender, EventArgs e)
        {
            LoadNodeData();
        }

        private void LoadNodeData()
        {
            metaViewer.Metadata.Clear();
            metaViewer.Visibility = Visibility.Hidden;

            if (scenario.SelectedNode == null)
                return;

            var type = (NodeType)scenario.SelectedNode.Tag;
            var paletteKey = PaletteType.FromNodeType(type);
            if (paletteKey != null && scenario.SelectedItemIndex >= 0)
            {
                var palette = scenario.Palettes[paletteKey];
                var baseAddress =  palette.PlacementBlockRef.TagBlock.Pointer.Address
                    + scenario.SelectedItemIndex * palette.PlacementBlockRef.BlockSize;

            }
            else
            {
                switch (type)
                {
                    case NodeType.Mission:
                        break;

                    default:
                        return;
                }
            }

            metaViewer.Visibility = Visibility.Visible;
        }
    }
}
