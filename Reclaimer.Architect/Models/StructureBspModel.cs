using Adjutant.Blam.Common;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Endian;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Reclaimer.Models
{
    public class StructureBspModel : BindableBase
    {
        internal bool IsBusy { get; private set; }
        internal Action<string, Exception> LogError { get; set; }

        public IIndexItem StructureBspTag { get; }

        private IStructureBspHierarchyView hierarchyView;
        public IStructureBspHierarchyView HierarchyView
        {
            get { return hierarchyView; }
            set
            {
                var prev = hierarchyView;
                if (SetProperty(ref hierarchyView, value))
                {
                    prev?.ClearScenario();
                    hierarchyView?.SetScenario(this);
                }
            }
        }

        private IStructureBspPropertyView propertyView;
        public IStructureBspPropertyView PropertyView
        {
            get { return propertyView; }
            set
            {
                var prev = propertyView;
                if (SetProperty(ref propertyView, value))
                {
                    prev?.ClearScenario();
                    propertyView?.SetScenario(this);
                }
            }
        }

        private IStructureBspRenderView renderView;
        public IStructureBspRenderView RenderView
        {
            get { return renderView; }
            set
            {
                var prev = renderView;
                if (SetProperty(ref renderView, value))
                {
                    prev?.ClearScenario();
                    renderView?.SetScenario(this);
                }
            }
        }

        public StructureBspModel(IIndexItem item)
        {
            IsBusy = true;

            StructureBspTag = item;

            IsBusy = false;
        }

        public void SaveChanges()
        {
            switch (StructureBspTag.CacheFile.CacheType)
            {
                case CacheType.Halo3Retail:
                case CacheType.MccHalo3:
                    SaveChangesHalo3();
                    break;
            }
        }

        private void SaveChangesHalo3()
        {
            var meta = StructureBspTag.ReadMetadata<Adjutant.Blam.Halo3.scenario_structure_bsp>();
            var baseAddress = meta.GeometryInstances.Pointer.Address;

            using (var fs = new FileStream(StructureBspTag.CacheFile.FileName, FileMode.Open, FileAccess.Write))
            using (var writer = new EndianWriter(fs, StructureBspTag.CacheFile.ByteOrder))
            {
                foreach (var placement in RenderView.GetPlacements())
                {
                    writer.Seek(baseAddress + 120 * placement.SourceIndex, SeekOrigin.Begin);

                    writer.Write(placement.TransformScale);
                    writer.Write(placement.M11);
                    writer.Write(placement.M12);
                    writer.Write(placement.M13);
                    writer.Write(placement.M21);
                    writer.Write(placement.M22);
                    writer.Write(placement.M23);
                    writer.Write(placement.M31);
                    writer.Write(placement.M32);
                    writer.Write(placement.M33);
                    writer.Write(placement.M41);
                    writer.Write(placement.M42);
                    writer.Write(placement.M43);
                    writer.Write((short)placement.MeshIndex);

                    writer.Seek(baseAddress + 120 * placement.SourceIndex + 64, SeekOrigin.Begin);
                    writer.Write(placement.SphereX);
                    writer.Write(placement.SphereY);
                    writer.Write(placement.SphereZ);
                    writer.Write(placement.SphereRadius);
                }
            }
        }
    }
}
