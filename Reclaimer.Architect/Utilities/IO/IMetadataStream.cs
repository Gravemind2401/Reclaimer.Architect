using Adjutant.Blam.Common;
using Adjutant.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Endian;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Utilities.IO
{
    internal interface IMetadataStream
    {
        bool IsInitialised { get; }
        IIndexItem SourceTag { get; }
        ICacheFile SourceCache { get; }
        ByteOrder ByteOrder { get; }
        IAddressTranslator AddressTranslator { get; }
        IPointerExpander PointerExpander { get; }
        void ResizeTagBlock(EndianWriter writer, ref TagBlock block, int entrySize, int newCount);
    }
}
