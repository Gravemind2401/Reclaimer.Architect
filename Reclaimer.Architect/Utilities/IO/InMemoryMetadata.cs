using Adjutant.Blam.Common;
using Adjutant.Blam.Common.Gen3;
using Adjutant.Utilities;
using Reclaimer.Plugins.MetaViewer;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Endian;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Reclaimer.Utilities.IO
{
    public class InMemoryMetadataStream : Stream
    {
        private InMemoryBlockCollection currentBlock;

        private IIndexItem SourceItem { get; }
        private InMemoryBlockCollection RootBlock { get; }
        private List<InMemoryBlockCollection> AllBlocks { get; }

        private int CurrentOffset => (int)(position - currentBlock?.VirtualAddress ?? 0);

        #region Stream Implementation
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => AllBlocks.Count > 0 ? AllBlocks.Max(b => b.VirtualAddress + b.AllocatedSize) : 0;

        private long position;
        public override long Position
        {
            get { return position; }
            set
            {
                if (position == value)
                    return;

                position = value;

                if (currentBlock?.ContainsVirtualAddress(position) == true)
                    return;

                currentBlock = AllBlocks.FirstOrDefault(b => b.ContainsVirtualAddress(position));
            }
        }
        #endregion

        private static XmlDocument GetDocument(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc;
        }

        public InMemoryMetadataStream(IIndexItem item, string xml)
            : this(item, GetDocument(xml))
        {

        }

        public InMemoryMetadataStream(IIndexItem item, XmlDocument doc)
        {
            SourceItem = item;
            AllBlocks = new List<InMemoryBlockCollection>();

            using (var reader = item.CacheFile.CreateReader(item.CacheFile.DefaultAddressTranslator))
            {
                var expander = (item.CacheFile as IMccCacheFile)?.PointerExpander;
                if (expander != null)
                    reader.RegisterInstance(expander);

                reader.Seek(item.MetaPointer.Address, SeekOrigin.Begin);
                RootBlock = ReadBlocks(reader, doc.DocumentElement, null, 0);
                //var largest = AllBlocks.OrderByDescending(b => b.TotalSize).ToList();
            }

            currentBlock = AllBlocks.FirstOrDefault(b => b.ContainsVirtualAddress(position));
        }

        private InMemoryBlockCollection ReadBlocks(EndianReader reader, XmlNode node, InMemoryBlockCollection parent, int parentBlockIndex)
        {
            var result = new InMemoryBlockCollection(SourceItem, reader, node, parent, parentBlockIndex);

            var lastBlock = AllBlocks.LastOrDefault();
            var nextAddress = (lastBlock?.VirtualAddress + lastBlock?.AllocatedSize) ?? 0;
            var nextSize = 0;
            while (nextSize < result.VirtualSize)
                nextSize += 1 << 16;

            result.Allocate(nextAddress, nextSize);
            AllBlocks.Add(result);

            var blockAddress = parent == null
                ? SourceItem.MetaPointer.Address
                : result.BlockRef.Pointer.Address;

            for (int i = 0; i < result.EntryCount; i++)
            {
                var indexAddress = blockAddress + result.EntrySize * i;
                foreach (var childNode in node.SelectNodes("tagblock").OfType<XmlNode>())
                {
                    reader.Seek(indexAddress + childNode.GetIntAttribute("offset").Value, SeekOrigin.Begin);
                    ReadBlocks(reader, childNode, result, i);
                }
            }

            return result;
        }
        
        public IBlockEditor GetBlockEditor(long address) => AllBlocks.FirstOrDefault(b => b.ContainsVirtualAddress(address));

        public void Commit()
        {
            using (var fs = new FileStream(SourceItem.CacheFile.FileName, FileMode.Open, FileAccess.Write))
            using (var writer = new EndianWriter(fs, SourceItem.CacheFile.ByteOrder))
            {
                foreach (var block in AllBlocks)
                    block.Commit(writer);
            }
        }

        #region Stream Implementation
        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                if (offset < 0)
                    throw new IOException("Attempted to seek before the beginning of the stream.");

                Position = offset;
            }
            else if (origin == SeekOrigin.End)
            {
                if (-offset > Length)
                    throw new IOException("Attempted to seek before the beginning of the stream.");

                Position = Length + offset;
            }
            else
            {
                if (-offset > Position)
                    throw new IOException("Attempted to seek before the beginning of the stream.");

                Position += offset;
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var totalRead = 0;

            while (count > 0)
            {
                var read = currentBlock?.Read(CurrentOffset, buffer, offset, count) ?? 0;
                if (read == 0)
                    break;

                count -= read;
                offset += read;
                Position += read;
                totalRead += read;
            }

            return totalRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                var write = currentBlock?.Write(CurrentOffset, buffer, offset, count) ?? 0;
                if (write == 0)
                    break;

                count -= write;
                offset += write;
                Position += write;
            }
        }
        #endregion
    }
}
