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
    /// Interaction logic for HierarchyView.xaml
    /// </summary>
    public partial class HierarchyView : UserControl
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

        public HierarchyView()
        {
            InitializeComponent();
            TabModel = new TabModel(this, Studio.Controls.TabItemType.Tool);
        }

        private void OnScenarioUnset()
        {
            DataContext = null;
        }

        private void OnScenarioSet()
        {
            DataContext = scenario;
        }

        private void tv_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            scenario.SelectedNode = tv.SelectedItem as TreeItemModel;
        }
    }
}
