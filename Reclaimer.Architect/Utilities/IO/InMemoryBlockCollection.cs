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
    //'block' refers to the whole chunk of data from the pointer address to (address + (size * count))
    //'entry' refers to the individual chunks within a block (there are [count] entries in a block)
    internal class InMemoryBlockCollection : IBlockEditor
    {
        private readonly ByteOrder byteOrder;
        private readonly IAddressTranslator translator;
        private readonly IPointerExpander expander;

        private byte[] data;

        public XmlNode XmlNode { get; }
        public InMemoryBlockCollection ParentBlock { get; }
        public List<InMemoryBlockCollection> ChildBlocks { get; } //all blocks from all pointers from all entries in this block
        public int OffsetInParent { get; } //the offset (in the parent entry) of the pointer to this block
        public int ParentEntryIndex { get; } //the index of the parent entry
        public string Name { get; }
        public TagBlock BlockRef { get; } //the blockref in the parent entry that points here
        public int EntrySize { get; }

        public int VirtualAddress { get; private set; }
        public int AllocatedSize { get; private set; }
        public int EntryCount { get; private set; }

        //tag root has no block ref and uses OffsetInParent/EntrySize instead
        public int PhysicalAddress => (int)(BlockRef?.Pointer.Address ?? OffsetInParent);
        public int PhysicalSize => (BlockRef?.Count ?? EntryCount) * EntrySize;

        public int VirtualSize => EntrySize * EntryCount;

        public InMemoryBlockCollection(IIndexItem sourceItem, EndianReader reader, XmlNode node, InMemoryBlockCollection parent, int parentBlockIndex)
        {
            byteOrder = sourceItem.CacheFile.ByteOrder;
            translator = sourceItem.CacheFile.DefaultAddressTranslator;
            expander = (sourceItem.CacheFile as IMccCacheFile)?.PointerExpander;

            XmlNode = node;
            ParentBlock = parent;
            ParentEntryIndex = parentBlockIndex;
            ChildBlocks = new List<InMemoryBlockCollection>();
            ParentBlock?.ChildBlocks.Add(this);

            if (parent == null) //tag root
            {
                OffsetInParent = (int)sourceItem.MetaPointer.Address;
                Name = sourceItem.FileName();
                BlockRef = null;
                EntryCount = 1;
                EntrySize = node.GetIntAttribute("baseSize") ?? 0;
                data = reader.ReadBytes(VirtualSize);
            }
            else
            {
                OffsetInParent = node.GetIntAttribute("offset") ?? 0;
                Name = node.GetStringAttribute("name");
                BlockRef = reader.ReadObject<TagBlock>();
                EntryCount = BlockRef.Count;
                EntrySize = node.GetIntAttribute("elementSize", "entrySize", "size") ?? 0;

                if (EntryCount > 0)
                {
                    reader.Seek(BlockRef.Pointer.Address, SeekOrigin.Begin);
                    data = reader.ReadBytes(VirtualSize);
                }
                else data = Array.Empty<byte>();
            }
        }

        public void Allocate(int address, int size)
        {
            if (size < VirtualSize)
                throw new ArgumentOutOfRangeException(nameof(size), size, "Insufficent memory allocation");

            VirtualAddress = address;
            AllocatedSize = size;

            if (ParentBlock == null)
                return;

            UpdateSourcePointer();
        }

        public bool ContainsPhysicalAddress(long address) => address >= PhysicalAddress && address < PhysicalAddress + PhysicalSize;

        public bool ContainsVirtualAddress(long address) => address >= VirtualAddress && address < VirtualAddress + VirtualSize;

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

        public void Remove(int index)
        {
            var entryOffset = index * EntrySize;
            var tailData = Read(entryOffset + EntrySize, (EntryCount - (index + 1)) * EntrySize);
            Resize(EntryCount - 1);
            Write(entryOffset, tailData);
        }

        public void Add() => Resize(EntryCount + 1);

        private void Insert(int index, byte[] entryData)
        {
            if (entryData != null && entryData.Length != EntrySize)
                throw new ArgumentException();

            var entryOffset = index * EntrySize;
            var tailData = Read(entryOffset, (EntryCount - index) * EntrySize);
            Resize(EntryCount + 1);
            if (entryData == null)
                Array.Clear(data, entryOffset, EntrySize);
            else Write(entryOffset, entryData);
            Write(entryOffset + EntrySize, tailData);
        }

        public void Insert(int index)
        {
            if (index > EntryCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (index == EntryCount)
                Add();
            else
                Insert(index, null);
        }

        public void Copy(int sourceIndex, int destIndex)
        {
            if (sourceIndex >= EntryCount)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));

            if (destIndex >= EntryCount)
                throw new ArgumentOutOfRangeException(nameof(destIndex));

            var sourceData = Read(sourceIndex * EntrySize, EntrySize);
            Insert(destIndex, sourceData);
        }

        public void Resize(int newCount)
        {
            if (newCount == EntryCount)
                return;

            var currentSize = VirtualSize;
            var newSize = newCount * EntrySize;

            if (newSize > data.Length)
                Array.Resize(ref data, newSize);
            else if (newCount > EntryCount) //if expanding into leftover data, zero it out
                Array.Clear(data, VirtualSize, newSize - currentSize);

            EntryCount = newCount;
            UpdateSourcePointer();
        }

        public void UpdateSourcePointer()
        {
            var newPointer = translator.GetPointer(VirtualAddress);
            if (expander != null)
                newPointer = expander.Contract(newPointer);

            var countBytes = BitConverter.GetBytes(EntryCount);
            var pointerBytes = BitConverter.GetBytes((int)newPointer);

            if (byteOrder == ByteOrder.BigEndian)
            {
                Array.Reverse(countBytes);
                Array.Reverse(pointerBytes);
            }

            var pointerAddress = ParentBlock.EntrySize * ParentEntryIndex + OffsetInParent;

            ParentBlock.Write(pointerAddress, countBytes);
            ParentBlock.Write(pointerAddress + 4, pointerBytes);
        }

        public void Commit(EndianWriter writer)
        {
            if (EntryCount == 0)
                return;

            var rootAddress = BlockRef?.Pointer.Address ?? OffsetInParent;

            writer.Seek(rootAddress, SeekOrigin.Begin);
            writer.Write(data);

            //restore original pointers
            foreach (var child in ChildBlocks)
            {
                writer.Seek(rootAddress + EntrySize * child.ParentEntryIndex + child.OffsetInParent + 4, SeekOrigin.Begin);
                writer.Write(child.BlockRef.Pointer.Value);
            }
        }

        public override string ToString() => Name;
    }
}
