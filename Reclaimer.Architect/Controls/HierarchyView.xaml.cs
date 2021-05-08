using Adjutant.Blam.Common;
using Reclaimer.Components;
using Reclaimer.Models;
using Reclaimer.Plugins;
using Reclaimer.Resources;
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
            TabModel = new TabModel(this, Studio.Controls.TabItemType.Tool) { Header = "Hierarchy", ToolTip = "Hierarchy View" };
        }

        public void ClearScenario()
        {
            DataContext = scenario = null;
        }

        public void SetScenario(ScenarioModel scenario)
        {
            DataContext = this.scenario = scenario;
        }

        public void ShowCurrentSelection()
        {
            //dont use indexing in case the list is empty
            if (scenario.SelectedItemIndex < 0)
                list.ScrollIntoView(scenario.Items.FirstOrDefault());
            else list.ScrollIntoView(scenario.Items.Skip(scenario.SelectedItemIndex).FirstOrDefault());
        }

        private void tv_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            scenario.SelectedNode = tv.SelectedItem as SceneNodeModel;
            var nodeType = scenario.SelectedNodeType;
            var handler = scenario.GetNodeTypeHandler(nodeType);
            btnAddItem.IsEnabled = handler?.SupportsObjectOperation(ObjectOperation.Add, nodeType) ?? false;
            btnDeleteItem.IsEnabled = handler?.SupportsObjectOperation(ObjectOperation.Remove, nodeType) ?? false;
            btnCopyItem.IsEnabled = handler?.SupportsObjectOperation(ObjectOperation.Copy, nodeType) ?? false;
        }

        private void ListItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            scenario.RenderView?.NavigateToObject(scenario.SelectedNode, scenario.SelectedItemIndex);
        }

        private void RecursiveToggle(IEnumerable<TreeItemModel> collection, bool value)
        {
            foreach (var item in collection)
            {
                item.IsExpanded = value;
                RecursiveToggle(item.Items, value);
            }
        }

        #region Toolbar Events

        private void btnCollapseAll_Click(object sender, RoutedEventArgs e)
        {
            RecursiveToggle(scenario.Hierarchy, false);
        }

        private void btnExpandAll_Click(object sender, RoutedEventArgs e)
        {
            RecursiveToggle(scenario.Hierarchy, true);
        }

        private void btnAddItem_Click(object sender, RoutedEventArgs e)
        {
            var treeNode = scenario.SelectedNode;
            var handler = scenario.GetNodeTypeHandler(treeNode.NodeType);
            if (handler.ExecuteObjectOperation(treeNode, ObjectOperation.Add, scenario.SelectedItemIndex))
            {
                scenario.RefreshItemList();
                list.ScrollIntoView(scenario.Items.LastOrDefault());
            }
        }

        private void btnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var treeNode = scenario.SelectedNode;
            var handler = scenario.GetNodeTypeHandler(treeNode.NodeType);
            if (handler.ExecuteObjectOperation(treeNode, ObjectOperation.Remove, scenario.SelectedItemIndex))
                scenario.RefreshItemList();
        }

        private void btnCopyItem_Click(object sender, RoutedEventArgs e)
        {
            var treeNode = scenario.SelectedNode;
            var handler = scenario.GetNodeTypeHandler(treeNode.NodeType);
            if (handler.ExecuteObjectOperation(treeNode, ObjectOperation.Copy, scenario.SelectedItemIndex))
                scenario.RefreshItemList();
        }

        private void btnEditPalette_Click(object sender, RoutedEventArgs e)
        {
            var paletteKey = PaletteType.FromNodeType(scenario.SelectedNodeType);
            if (paletteKey != null)
                new PaletteEditorWindow(scenario, paletteKey) { Owner = Window.GetWindow(this) }.ShowDialog();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                scenario.MetadataStream.Commit();
            }
            catch (Exception ex)
            {
                scenario.LogError($"Unable to save changes to {scenario.ScenarioTag.FileName()}", ex, true);
                Substrate.ShowErrorMessage(ex.Message);
            }
        }

        #endregion
    }
}
