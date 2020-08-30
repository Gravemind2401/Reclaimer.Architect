using Adjutant.Geometry;
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
using System.Xml;

namespace Reclaimer.Controls
{
    /// <summary>
    /// Interaction logic for MetaViewer.xaml
    /// </summary>
    public partial class MiniMetaViewer
    {
        #region Dependency Properties
        public static readonly DependencyProperty ShowInvisiblesProperty =
            DependencyProperty.Register(nameof(ShowInvisibles), typeof(bool), typeof(MiniMetaViewer), new PropertyMetadata(false, ShowInvisiblesChanged));

        public bool ShowInvisibles
        {
            get { return (bool)GetValue(ShowInvisiblesProperty); }
            set { SetValue(ShowInvisiblesProperty, value); }
        }

        public static void ShowInvisiblesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //MetaViewerPlugin.Settings.ShowInvisibles = e.NewValue as bool? ?? false;
        }
        #endregion

        private MetaContext context;
        private XmlNode rootNode;
        private long baseAddress;

        private readonly Dictionary<string, MetaValueBase> valuesById;

        public ObservableCollection<MetaValueBase> Metadata { get; }

        public MiniMetaViewer()
        {
            InitializeComponent();
            valuesById = new Dictionary<string, MetaValueBase>();

            Metadata = new ObservableCollection<MetaValueBase>();
            DataContext = this;
            //ShowInvisibles = MetaViewerPlugin.Settings.ShowInvisibles;
        }

        public void LoadMetadata(MetaContext context, XmlNode layout, long baseAddress)
        {
            this.context = context;
            rootNode = layout;
            this.baseAddress = baseAddress;

            LoadData();
        }

        private void LoadData()
        {
            if (context == null || rootNode == null)
                return;

            Metadata.Clear();
            valuesById.Clear();
            foreach (XmlNode n in rootNode.ChildNodes)
            {
                try
                {
                    var meta = MetaValueBase.GetMetaValue(n, context, baseAddress);
                    Metadata.Add(meta);
                    if (n.Attributes["id"] != null)
                        valuesById.Add(n.Attributes["id"].Value, meta);
                }
                catch { }
            }
        }

        public void SetValue(string id, object value)
        {
            if (!valuesById.ContainsKey(id))
                return;

            var meta = valuesById[id];

            var single = meta as SimpleValue;
            if (single != null)
            {
                single.Value = value;
                return;
            }

            var multi = meta as MultiValue;
            if (multi != null)
            {
                var vector = (IXMVector)value;
                multi.Value1 = vector.X;
                multi.Value2 = vector.Y;
                multi.Value3 = vector.Z;
                multi.Value4 = vector.W;
            }
        }

        private void RecursiveToggle(IEnumerable<MetaValueBase> collection, bool value)
        {
            foreach (var s in collection.OfType<IExpandable>())
            {
                s.IsExpanded = value;
                RecursiveToggle(s.Children, value);
            }
        }

        private void btnReload_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void btnCollapseAll_Click(object sender, RoutedEventArgs e)
        {
            RecursiveToggle(Metadata, false);
        }

        private void btnExpandAll_Click(object sender, RoutedEventArgs e)
        {
            RecursiveToggle(Metadata, true);
        }
    }
}
