﻿using Reclaimer.Models;
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
    public partial class HierarchyView : IScenarioHierarchyView
    {
        public TabModel TabModel { get; }

        private ScenarioModel scenario;

        public HierarchyView()
        {
            InitializeComponent();
            TabModel = new TabModel(this, Studio.Controls.TabItemType.Tool) { Header = "Hierarchy" };
        }

        public void ClearScenario()
        {
            DataContext = scenario = null;
        }

        public void SetScenario(ScenarioModel scenario)
        {
            DataContext = this.scenario = scenario;
        }

        private void tv_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            scenario.SelectedNode = tv.SelectedItem as TreeItemModel;
        }

        private void ListItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var type = (NodeType)scenario.SelectedNode.Tag;
            scenario.RenderView?.NavigateToObject(type, scenario.SelectedItemIndex);
        }
    }
}