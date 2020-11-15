using Adjutant.Blam.Common;
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

namespace Reclaimer.Utilities
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

                if (currentBlock?.ContainsAddress(position) == true)
                    return;

                currentBlock = AllBlocks.FirstOrDefault(b => b.ContainsAddress(position));
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
                reader.Seek(item.MetaPointer.Address, SeekOrigin.Begin);
                RootBlock = ReadBlocks(reader, doc.DocumentElement, null);
                //var largest = AllBlocks.OrderByDescending(b => b.TotalSize).ToList();
            }

            currentBlock = AllBlocks.FirstOrDefault(b => b.ContainsAddress(position));
        }

        private InMemoryBlockCollection ReadBlocks(EndianReader reader, XmlNode node, InMemoryBlockCollection parent)
        {
            var result = new InMemoryBlockCollection(SourceItem, reader, node, parent);

            var lastBlock = AllBlocks.LastOrDefault();
            var nextAddress = (lastBlock?.VirtualAddress + lastBlock?.AllocatedSize) ?? 0;
            var nextSize = 0;
            while (nextSize < result.TotalSize)
                nextSize += 1 << 16;

            result.Allocate(nextAddress, nextSize, SourceItem.CacheFile.ByteOrder, SourceItem.CacheFile.DefaultAddressTranslator);
            AllBlocks.Add(result);

            var blockAddress = parent == null
                ? SourceItem.MetaPointer.Address
                : result.BlockRef.Pointer.Address;

            for (int i = 0; i < result.Count; i++)
            {
                foreach (var childNode in node.SelectNodes("tagblock").OfType<XmlNode>())
                {
                    reader.Seek(blockAddress + childNode.GetIntAttribute("offset").Value, SeekOrigin.Begin);
                    ReadBlocks(reader, childNode, result);
                }
            }

            return result;
        }

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

    internal class InMemoryBlockCollection
    {
        private byte[] data;

        public XmlNode XmlNode { get; }
        public InMemoryBlockCollection ParentBlock { get; }
        public List<InMemoryBlockCollection> ChildBlocks { get; }
        public int OffsetInParent { get; }
        public string Name { get; }
        public TagBlock BlockRef { get; }
        public int BlockSize { get; }

        public int VirtualAddress { get; private set; }
        public int AllocatedSize { get; private set; }
        public int Count { get; private set; }

        public int TotalSize => BlockSize * Count;

        public InMemoryBlockCollection(IIndexItem sourceItem, EndianReader reader, XmlNode node, InMemoryBlockCollection parent)
        {
            XmlNode = node;
            ParentBlock = parent;
            ChildBlocks = new List<InMemoryBlockCollection>();

            if (parent == null) //tag root
            {
                OffsetInParent = (int)sourceItem.MetaPointer.Address;
                Name = sourceItem.FileName();
                BlockRef = null;
                Count = 1;
                BlockSize = node.GetIntAttribute("baseSize") ?? 0;
                data = reader.ReadBytes(TotalSize);
            }
            else
            {
                OffsetInParent = node.GetIntAttribute("offset") ?? 0;
                Name = node.GetStringAttribute("name");
                BlockRef = reader.ReadObject<TagBlock>();
                Count = BlockRef.Count;
                BlockSize = node.GetIntAttribute("elementSize", "entrySize", "size") ?? 0;
                reader.Seek(BlockRef.Pointer.Address, SeekOrigin.Begin);
                data = reader.ReadBytes(TotalSize);
                ParentBlock.ChildBlocks.Add(this);
            }
        }

        public void Allocate(int address, int size, ByteOrder byteOrder, IAddressTranslator translator)
        {
            if (size < TotalSize)
                throw new ArgumentOutOfRangeException(nameof(size), size, "Insufficent memory allocation");

            VirtualAddress = address;
            AllocatedSize = size;

            if (ParentBlock == null)
                return;

            var newPointer = translator.GetPointer(VirtualAddress);
            var bits = BitConverter.GetBytes((int)newPointer);

            if (byteOrder == ByteOrder.BigEndian)
                Array.Reverse(bits);

            ParentBlock.Write(OffsetInParent + 4, bits);
        }

        public bool ContainsAddress(long address) => address >= VirtualAddress && address < VirtualAddress + AllocatedSize;

        public byte[] Read(int position, int count)
        {
            count = Math.Min(count, AllocatedSize - position);
            var result = new byte[count];
            Array.Copy(data, position, result, 0, count);
            return result;
        }

        public int Read(int position, byte[] buffer, int offset, int count)
        {
            count = Math.Min(count, AllocatedSize - position);
            Array.Copy(data, position, buffer, offset, count);
            return count;
        }

        public int Write(int position, byte[] buffer) => Write(position, buffer, 0, buffer.Length);

        public int Write(int position, byte[] buffer, int offset, int count)
        {
            count = Math.Min(count, AllocatedSize - position);
            Array.Copy(buffer, offset, data, position, count);
            return count;
        }

        public void RemoveAt(int index)
        {
            var blockOffset = index * BlockSize;
            var tailData = Read(blockOffset + BlockSize, (Count - (index + 1)) * BlockSize);
            Resize(Count - 1);
            Write(blockOffset, tailData);
        }

        public void Insert(int index)
        {
            var blockOffset = index * BlockSize;
            var tailData = Read(blockOffset, (Count - index) * BlockSize);
            Resize(Count + 1);
            Array.Clear(data, blockOffset, BlockSize);
            Write(blockOffset + BlockSize, tailData);
        }

        public void Resize(int newCount)
        {
            if (newCount == Count)
                return;

            var currentSize = TotalSize;
            var newSize = newCount * BlockSize;

            if (newSize > data.Length)
                Array.Resize(ref data, newSize);
            else if (newCount > Count) //if expanding into leftover data, zero it out
                Array.Clear(data, TotalSize, newSize - currentSize);

            Count = newCount;
        }

        public void Commit(EndianWriter writer)
        {
            var rootAddress = BlockRef?.Pointer.Address ?? OffsetInParent;

            writer.Seek(rootAddress, SeekOrigin.Begin);
            writer.Write(data);

            //restore original pointers
            foreach (var child in ChildBlocks)
            {
                writer.Seek(rootAddress + child.OffsetInParent + 4, SeekOrigin.Begin);
                writer.Write(child.BlockRef.Pointer.Value);
            }
        }

        public override string ToString() => Name;
    }
}
