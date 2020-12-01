using Adjutant.Blam.Common;
using Reclaimer.Models;
using Reclaimer.Plugins.MetaViewer;
using Reclaimer.Plugins.MetaViewer.Halo3;
using Reclaimer.Resources;
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

namespace Reclaimer.Controls
{
    /// <summary>
    /// Interaction logic for PaletteEditor.xaml
    /// </summary>
    public partial class PaletteEditorWindow : Window, IMetaViewerHost
    {
        private readonly string paletteKey;
        private readonly ScenarioModel scenario;
        private readonly MetaContext context;

        public ObservableCollection<MetaValueBase> Metadata { get; }

        public bool ShowInvisibles => true;

        internal PaletteEditorWindow(ScenarioModel scenario, string paletteKey)
            : this()
        {
            this.scenario = scenario;
            this.paletteKey = paletteKey;
            context = new MetaContext(scenario.Xml, scenario.ScenarioTag.CacheFile, scenario.ScenarioTag, scenario.MetadataStream);

            var palette = scenario.Palettes[paletteKey];
            var blockRef = palette.PaletteBlockRef;
            var blockAddress = blockRef.TagBlock.Pointer.Address;
            var tagRefNode = palette.PaletteNode.SelectSingleNode($"*[@id='{FieldId.TagReference}']");
            var tagRefOffset = tagRefNode.GetIntAttribute("offset") ?? 0;

            for (int i = 0; i < blockRef.TagBlock.Count; i++)
            {
                var baseAddress = blockAddress + blockRef.BlockSize * i + tagRefOffset;
                var meta = MetaValueBase.GetMetaValue(tagRefNode, context, baseAddress);
                meta.PropertyChanged += Meta_PropertyChanged;
                Metadata.Add(meta);
            }

            DataContext = this;
        }

        public PaletteEditorWindow()
        {
            InitializeComponent();
            Metadata = new ObservableCollection<MetaValueBase>();
        }

        private void Meta_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var item = (sender as TagReferenceValue).SelectedItem.Context;
            var tagRef = new TagReference(item.CacheFile, item.ClassId, item.Id);

            var index = Metadata.IndexOf(sender as MetaValueBase);
            scenario.Palettes[paletteKey].Palette[index] = tagRef;
            scenario.RenderView.RefreshPalette(paletteKey, index);
        }

        #region Toolbar Events

        private void btnReload_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnAddItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnDeleteItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion
    }
}
