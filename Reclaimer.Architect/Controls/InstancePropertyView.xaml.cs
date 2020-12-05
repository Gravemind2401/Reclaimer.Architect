using Adjutant.Geometry;
using Adjutant.Spatial;
using Reclaimer.Models;
using Reclaimer.Plugins.MetaViewer;
using Reclaimer.Plugins.MetaViewer.Halo3;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Xml;

namespace Reclaimer.Controls
{
    /// <summary>
    /// Interaction logic for InstancePropertyView.xaml
    /// </summary>
    public partial class InstancePropertyView : IStructureBspPropertyView
    {
        public static readonly DependencyProperty CurrentItemProperty =
            DependencyProperty.Register(nameof(CurrentItem), typeof(InstancePlacement), typeof(InstancePropertyView), new PropertyMetadata(null, (d, e) =>
            {
                (d as FrameworkElement).Visibility = e.NewValue == null ? Visibility.Hidden : Visibility.Visible;
            }));

        public InstancePlacement CurrentItem
        {
            get { return (InstancePlacement)GetValue(CurrentItemProperty); }
            set { SetValue(CurrentItemProperty, value); }
        }

        private StructureBspModel bspModel;

        public TabModel TabModel { get; }

        public InstancePropertyView()
        {
            InitializeComponent();
            TabModel = new TabModel(this, Studio.Controls.TabItemType.Tool) { Header = "Properties", ToolTip = "Property View" };
        }

        public void ClearScenario()
        {
            CurrentItem = null;
            Visibility = Visibility.Hidden;
            bspModel = null;
        }

        public void SetScenario(StructureBspModel bspModel)
        {
            CurrentItem = null;
            Visibility = Visibility.Hidden;
            this.bspModel = bspModel;
        }
    }
}
