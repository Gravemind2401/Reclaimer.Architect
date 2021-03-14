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
        private readonly IMetadataStream stream;
        private readonly Dictionary<int, byte[]> defaultValues;

        private bool hasChanged;
        private byte[] data;
        private TagBlock blockRef;

        public XmlNode XmlNode { get; }
        public InMemoryBlockCollection ParentBlock { get; }
        public List<InMemoryBlockCollection> ChildBlocks { get; } //all blocks from all pointers from all entries in this block

        //the offset (in the parent entry) of the pointer to this block
        private readonly int? offsetInParent; //null for tag root
        public int OffsetInParent => offsetInParent ?? (int)stream.SourceTag.MetaPointer.Address;

        public int ParentEntryIndex { get; } //the index of the parent entry
        public string Name { get; }
        public TagBlock BlockRef => blockRef; //the (physical) blockref in the parent entry that points here
        public int EntrySize { get; }

        public int VirtualAddress { get; private set; }
        public int AllocatedSize { get; private set; }
        public int EntryCount { get; private set; }

        //tag root has no block ref and uses OffsetInParent/EntrySize instead
        public int PhysicalAddress => (int)(BlockRef?.Pointer.Address ?? OffsetInParent);
        public int PhysicalSize => (BlockRef?.Count ?? EntryCount) * EntrySize;

        public int VirtualSize => EntrySize * EntryCount;

        public InMemoryBlockCollection(IMetadataStream stream, EndianReader reader, XmlNode node, InMemoryBlockCollection parent, int parentBlockIndex)
        {
            this.stream = stream;
            defaultValues = new Dictionary<int, byte[]>();

            XmlNode = node;
            ParentBlock = parent;
            ParentEntryIndex = parentBlockIndex;
            ChildBlocks = new List<InMemoryBlockCollection>();
            ParentBlock?.ChildBlocks.Add(this);

            if (parent == null) //tag root
            {
                Name = stream.SourceTag.FileName();
                blockRef = null;
                EntryCount = 1;
                EntrySize = node.GetIntAttribute("baseSize") ?? 0;
                data = reader.ReadBytes(VirtualSize);
            }
            else
            {
                offsetInParent = node.GetIntAttribute("offset") ?? 0;
                Name = node.GetStringAttribute("name");
                blockRef = reader.ReadObject<TagBlock>();
                EntryCount = BlockRef.Count;
                EntrySize = node.GetIntAttribute("elementSize", "entrySize", "size") ?? 0;

                if (EntryCount > 0)
                {
                    reader.Seek(BlockRef.Pointer.Address, SeekOrigin.Begin);
                    data = reader.ReadBytes(VirtualSize);
                }
                else data = Array.Empty<byte>();
            }

            foreach (var element in node.SelectNodes("*[@offset][@defaultValue]").OfType<XmlNode>())
            {
                var offset = element.GetIntAttribute("offset").Value;
                var defaultValue = element.GetStringAttribute("defaultValue");

                Func<string, int> getFlagValue = (val) => val.Split('|').Select(s => 1 << int.Parse(s)).Sum();

                byte[] bytes;
                switch (element.Name.ToLower())
                {
                    case "int8":
                    case "enum8":
                    case "blockindex8":
                        bytes = new[] { defaultValue == "-1" ? byte.MaxValue : byte.Parse(defaultValue) };
                        break;
                    case "int16":
                    case "blockindex16":
                        bytes = BitConverter.GetBytes(short.Parse(defaultValue));
                        break;
                    case "uint16":
                    case "enum16":
                        bytes = BitConverter.GetBytes(ushort.Parse(defaultValue));
                        break;
                    case "int32":
                    case "blockindex32":
                        bytes = BitConverter.GetBytes(int.Parse(defaultValue));
                        break;
                    case "uint32":
                    case "enum32":
                        bytes = BitConverter.GetBytes(uint.Parse(defaultValue));
                        break;
                    case "float32":
                        bytes = BitConverter.GetBytes(float.Parse(defaultValue));
                        break;
                    case "flags8":
                        bytes = BitConverter.GetBytes((byte)getFlagValue(defaultValue));
                        break;
                    case "flags16":
                        bytes = BitConverter.GetBytes((ushort)getFlagValue(defaultValue));
                        break;
                    case "flags32":
                        bytes = BitConverter.GetBytes((uint)getFlagValue(defaultValue));
                        break;
                    default:
                        continue;
                }

                if (stream.ByteOrder == ByteOrder.BigEndian)
                    Array.Reverse(bytes);

                defaultValues.Add(offset, bytes);
            }
        }

        public void Allocate(int address, int size)
        {
            if (size < VirtualSize)
                throw new ArgumentOutOfRangeException(nameof(size), size, "Insufficent memory allocation");

            VirtualAddress = address;
            AllocatedSize = size;

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
            OnChanged();
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

            //if adding new blocks, prefill the default values
            for (int i = EntryCount; i < newCount; i++)
            {
                foreach (var pair in defaultValues)
                    Write(i * EntrySize + pair.Key, pair.Value);
            }

            EntryCount = newCount;
            UpdateSourcePointer();
            OnChanged();
        }

        //update the virtual pointer in the parent block to make sure it still
        //points to the correct address if this block has moved or changed size
        public void UpdateSourcePointer()
        {
            if (ParentBlock == null)
                return;

            var newPointer = stream.AddressTranslator.GetPointer(VirtualAddress);
            if (stream.PointerExpander != null)
                newPointer = stream.PointerExpander.Contract(newPointer);

            var countBytes = BitConverter.GetBytes(EntryCount);
            var pointerBytes = BitConverter.GetBytes((int)newPointer);

            if (stream.ByteOrder == ByteOrder.BigEndian)
            {
                Array.Reverse(countBytes);
                Array.Reverse(pointerBytes);
            }

            var refOffset = ParentBlock.EntrySize * ParentEntryIndex + OffsetInParent;

            ParentBlock.Write(refOffset, countBytes);
            ParentBlock.Write(refOffset + 4, pointerBytes);
        }

        public void Commit(EndianWriterEx writer)
        {
            if (!hasChanged)
                return;

            if (BlockRef != null && EntryCount != BlockRef.Count)
            {
                stream.ResizeTagBlock(writer, ref blockRef, EntrySize, EntryCount);

                //we dont know if our parent has already been written (or even will be)
                //just to be sure, we need to go back and update it with the new reference
                var refOffset = ParentBlock.EntrySize * ParentEntryIndex + OffsetInParent;
                writer.Seek(ParentBlock.PhysicalAddress + refOffset, SeekOrigin.Begin);
                BlockRef.Write(writer, null);
            }

            var rootAddress = BlockRef?.Pointer.Address ?? OffsetInParent;

            if (EntryCount > 0)
            {
                writer.Seek(rootAddress, SeekOrigin.Begin);
                writer.Write(data); //this writes our virtual pointers to disk (fix them below)
            }

            //the block references in our data array currently use virtual pointers
            //this restores the original pointers that got overwritten above
            foreach (var child in ChildBlocks)
            {
                writer.Seek(rootAddress + EntrySize * child.ParentEntryIndex + child.OffsetInParent + 4, SeekOrigin.Begin);
                writer.Write(child.BlockRef.Pointer.Value);
            }

            hasChanged = false;
        }

        //bindings cause this to get hit as soon as you select an object
        //but its better than overwriting every block in the whole tag
        private void OnChanged()
        {
            if (!hasChanged && stream.IsInitialised)
                hasChanged = true;
        }

        public override string ToString() => Name;
    }
}
