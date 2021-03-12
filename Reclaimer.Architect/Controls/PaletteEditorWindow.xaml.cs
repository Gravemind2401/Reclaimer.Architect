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

            Reload();

            DataContext = this;
        }

        public PaletteEditorWindow()
        {
            InitializeComponent();
            Metadata = new ObservableCollection<MetaValueBase>();
        }

        private void Reload()
        {
            var palette = scenario.Palettes[paletteKey];
            var blockRef = palette.PaletteBlockRef;
            var blockAddress = blockRef.TagBlock.Pointer.Address;
            var tagRefNode = palette.PaletteNode.SelectSingleNode($"*[@id='{FieldId.TagReference}']");
            var tagRefOffset = tagRefNode.GetIntAttribute("offset") ?? 0;

            Metadata.Clear();
            for (int i = 0; i < blockRef.TagBlock.Count; i++)
            {
                var baseAddress = blockAddress + blockRef.BlockSize * i + tagRefOffset;
                var meta = (TagReferenceValue)MetaValueBase.GetMetaValue(tagRefNode, context, baseAddress);

                if ((meta.SelectedItem?.Context?.Id ?? 0) == 0)
                    meta.SelectedClass = meta.ClassOptions.FirstOrDefault(ci => ci.Label.ToLower() == paletteKey) ?? meta.ClassOptions.FirstOrDefault();

                meta.PropertyChanged += Meta_PropertyChanged;
                Metadata.Add(meta);
            }
        }

        private void Meta_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var item = (sender as TagReferenceValue).SelectedItem?.Context;
            var tagRef = item == null ? TagReference.NullReference : new TagReference(item.CacheFile, item.ClassId, item.Id);

            var index = Metadata.IndexOf(sender as MetaValueBase);
            scenario.Palettes[paletteKey].Palette[index] = tagRef;
            scenario.RenderView?.RefreshPalette(paletteKey, index);
            scenario.PropertyView?.Reload();
        }

        #region Toolbar Events

        private void btnAddItem_Click(object sender, RoutedEventArgs e)
        {
            var palette = scenario.Palettes[paletteKey];
            var blockRef = palette.PaletteBlockRef;
            var nextIndex = blockRef.TagBlock.Count;

            var blockEditor = scenario.MetadataStream.GetBlockEditor(blockRef.TagBlock.Pointer.Address);
            blockEditor.Add();

            blockRef.TagBlock = new TagBlock(blockEditor.EntryCount, blockRef.TagBlock.Pointer);
            scenario.Palettes[paletteKey].Palette.Add(TagReference.NullReference);
            scenario.RenderView.RefreshPalette(paletteKey, nextIndex);

            scenario.PropertyView?.Reload();
            Reload();
        }

        private void btnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var palette = scenario.Palettes[paletteKey];
            var blockRef = palette.PaletteBlockRef;
            var nextIndex = blockRef.TagBlock.Count - 1;

            var blockEditor = scenario.MetadataStream.GetBlockEditor(blockRef.TagBlock.Pointer.Address);
            blockEditor.Remove(nextIndex);

            blockRef.TagBlock = new TagBlock(blockEditor.EntryCount, blockRef.TagBlock.Pointer);
            scenario.Palettes[paletteKey].Palette.RemoveAt(nextIndex);
            scenario.RenderView.RefreshPalette(paletteKey, nextIndex);

            scenario.PropertyView?.Reload();
            Reload();
        }

        #endregion
    }
}
