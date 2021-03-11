﻿using Adjutant.Blam.Common;
using Adjutant.Blam.Common.Gen3;
using Adjutant.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Endian;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Blam.Common
{
    //based on https://github.com/XboxChaos/Assembly/blob/dev/src/Blamite/IO/FileSegmenter.cs
    public class CacheSegmenter
    {
        private readonly IGen3CacheFile cache;
        private readonly List<Segment> segments = new List<Segment>();

        private readonly Segment resourceSegment;
        private readonly Segment metadataSegment;

        private readonly Segment stringSegment0;
        private readonly Segment stringSegment1;
        private readonly Segment stringSegment2;
        private readonly Segment stringSegment3;
        private readonly Segment stringSegment4;

        private Segment eofSegment => segments.Last();

        private readonly SegmentGroup stringsGroup = new SegmentGroup();
        private readonly SegmentGroup localesGroup = new SegmentGroup();

        public CacheSegmenter(IGen3CacheFile cache)
        {
            if (cache.CacheType < CacheType.Halo3Retail || cache.CacheType >= CacheType.Halo4Beta)
                throw new NotSupportedException();

            this.cache = cache;

            var debugTranslator = new SectionAddressTranslator(cache, 0);
            var resourceTranslator = new SectionAddressTranslator(cache, 1);
            var metadataTranslator = new SectionAddressTranslator(cache, 2);
            var localeTranslator = new SectionAddressTranslator(cache, 3);

            //header
            segments.Add(new Segment(0, cache.GetHeaderSize(), 1));

            //eof
            segments.Add(new Segment((int)cache.Header.FileSize, 0, 1));

            //sections
            var origin = (int)resourceTranslator.GetAddress(resourceTranslator.VirtualAddress);
            var size = (int)cache.Header.SectionTable[1].Size;
            segments.Add(resourceSegment = new Segment(origin, size, 0x1000));

            origin = (int)metadataTranslator.GetAddress(metadataTranslator.VirtualAddress);
            size = cache.Header.VirtualSize;
            segments.Add(metadataSegment = new Segment(origin, size, 0x10000));

            //strings
            origin = (int)debugTranslator.GetAddress(cache.Header.StringTableIndexPointer.Value);
            size = cache.Header.StringCount * 4;
            segments.Add(stringsGroup.Add(stringSegment0 = new Segment(origin, size, 4)));

            origin = (int)debugTranslator.GetAddress(cache.Header.StringTablePointer.Value);
            size = cache.Header.StringTableSize;
            segments.Add(stringsGroup.Add(stringSegment1 = new Segment(origin, size, cache.UsesStringEncryption ? 16 : 1)));

            //tag names
            origin = (int)debugTranslator.GetAddress(cache.Header.FileTableIndexPointer.Value);
            size = cache.Header.FileCount * 4;
            segments.Add(stringsGroup.Add(stringSegment2 = new Segment(origin, size, 4)));

            origin = (int)debugTranslator.GetAddress(cache.Header.FileTablePointer.Value);
            size = cache.Header.FileTableSize;
            segments.Add(stringsGroup.Add(stringSegment3 = new Segment(origin, size, 1)));

            //only exists in mcc reach (U3+)
            if (cache.Header.StringNamespaceTablePointer.Value != 0)
            {
                origin = (int)debugTranslator.GetAddress(cache.Header.StringNamespaceTablePointer.Value);
                size = cache.Header.StringNamespaceCount * 4;
                segments.Add(stringSegment4 = stringsGroup.Add(new Segment(origin, size, 4)));
            }

            foreach (var def in cache.LocaleIndex.Languages.Where(def => def.StringCount > 0))
            {
                origin = (int)localeTranslator.GetAddress(def.IndicesOffset);
                size = def.StringCount * 8;
                segments.Add(localesGroup.Add(new Segment(origin, size, 8)));

                origin = (int)localeTranslator.GetAddress(def.StringsOffset);
                size = def.StringsSize;
                segments.Add(localesGroup.Add(new Segment(origin, size, cache.UsesStringEncryption ? 16 : 1)));
            }

            segments.Sort((a, b) => a.Offset.CompareTo(b.Offset));

            for (int i = 0; i < segments.Count - 1; i++)
                segments[i].NextSegment = segments[i + 1];
        }

        public void AddMetadata(int length)
        {
            int _;
            AddMetadata(length, out _, out _);
        }

        public void AddMetadata(int length, out int insertedAt, out int insertedCount)
        {
            using (var writer = cache.CreateWriter())
                AddMetadata(writer, length, out insertedAt, out insertedCount);
        }

        public void AddMetadata(EndianWriterEx writer, int length, out int insertedAt, out int insertedCount)
        {
            length = metadataSegment.Expand(length);

            cache.Header.FileSize = eofSegment.Offset;

            var debugTranslator = new SectionAddressTranslator(cache, 0);
            //var metadataTranslator = new SectionAddressTranslator(cache, 2);

            var remaining = metadataSegment.AvailableSize - cache.Header.PartitionTable.Skip(1).Sum(p => (int)p.Size);
            var pointer = cache.DefaultAddressTranslator.GetPointer(metadataSegment.Offset) - length; //DefaultAddressTranslator should always be the MetadataTranslator
            cache.Header.PartitionTable[0].Size = (ulong)remaining;
            cache.Header.PartitionTable[0].Address = (ulong)pointer;

            var debugSection = cache.Header.SectionTable[0];
            var resourceSection = cache.Header.SectionTable[1];
            var metadataSection = cache.Header.SectionTable[2];
            var localesSection = cache.Header.SectionTable[3];

            resourceSection.Size = (uint)resourceSegment.AvailableSize;

            localesSection.Address = resourceSection.Address + resourceSection.Size;
            localesSection.Size = (uint)localesGroup.AvailableSize;

            metadataSection.Address = resourceSection.Address + resourceSection.Size + localesSection.Size;
            metadataSection.Size = (uint)metadataSegment.AvailableSize;

            debugSection.Address = resourceSection.Address + resourceSection.Size + localesSection.Size + metadataSection.Size;
            debugSection.Size = (uint)stringsGroup.AvailableSize;

            if (debugSection.Address != 0 && cache.Header.SectionOffsetTable[0] != 0)
                debugSection.Address -= (uint)cache.Header.PartitionTable[0].Size;

            var offsets = cache.Header.SectionOffsetTable;
            offsets[0] = (uint)(stringsGroup.Offset - debugSection.Address);
            offsets[1] = (uint)(resourceSegment.Offset - resourceSection.Address);
            offsets[2] = (uint)(metadataSegment.Offset - metadataSection.Address);
            offsets[3] = (uint)(localesGroup.Offset - localesSection.Address);

            cache.Header.TagDataAddress = (int)metadataSection.Address;
            cache.Header.VirtualBaseAddress = pointer;
            //index header address
            cache.Header.VirtualSize = metadataSegment.AvailableSize;

            pointer = debugTranslator.GetPointer(stringSegment0.Offset);
            cache.Header.StringTableIndexPointer = new Pointer((int)pointer, cache.Header.StringTableIndexPointer);

            pointer = debugTranslator.GetPointer(stringSegment1.Offset);
            cache.Header.StringTablePointer = new Pointer((int)pointer, cache.Header.StringTablePointer);

            pointer = debugTranslator.GetPointer(stringSegment2.Offset);
            cache.Header.FileTableIndexPointer = new Pointer((int)pointer, cache.Header.FileTableIndexPointer);

            pointer = debugTranslator.GetPointer(stringSegment3.Offset);
            cache.Header.FileTablePointer = new Pointer((int)pointer, cache.Header.FileTablePointer);

            if (cache.Header.StringNamespaceTablePointer.Value != 0)
            {
                pointer = debugTranslator.GetPointer(stringSegment4.Offset);
                cache.Header.StringNamespaceTablePointer = new Pointer((int)pointer, cache.Header.StringNamespaceTablePointer);
            }

            insertedAt = metadataSegment.Offset;
            insertedCount = length;

            writer.Seek(0, SeekOrigin.Begin);
            writer.WriteObject(cache.Header, (int)cache.CacheType);

            writer.Seek(metadataSegment.Offset, SeekOrigin.Begin);
            writer.Insert(0, length);
        }

        private class Segment
        {
            private int offset;
            public int Offset
            {
                get { return offset; }
                set
                {
                    offset = Align(value, OffsetAlignment);
                    VerifySize();
                }
            }

            private int size;
            public int Size
            {
                get { return size; }
                set
                {
                    size = Align(value, SizeAlignment);
                    VerifySize();
                }
            }

            public int OffsetAlignment { get; }

            public int SizeAlignment { get; }

            public int AvailableSize => NextSegment == null ? Size : NextSegment.Offset - Offset;

            internal Segment NextSegment { get; set; }

            public Segment(int offset, int size, int sizeAlignment)
                : this(offset, size, sizeAlignment, 0x1000)
            { }

            public Segment(int offset, int size, int sizeAlignment, int offsetAlignment)
            {
                this.offset = offset;
                this.size = size;
                OffsetAlignment = offsetAlignment;
                SizeAlignment = sizeAlignment;
            }

            public int Expand(int minIncrease)
            {
                var increase = Align(minIncrease, SizeAlignment);
                Size += increase;
                return increase;
            }

            private int Align(int value, int blockSize)
            {
                var mod = value % blockSize;
                if (mod == 0)
                    return value;
                else return blockSize * (value / blockSize) + blockSize;
            }

            private void VerifySize()
            {
                if (Size <= AvailableSize || NextSegment == null)
                    return;

                NextSegment.Offset += Size - AvailableSize;
            }

            public override string ToString() => $"{Offset} - {Offset + AvailableSize} ({Size} / {AvailableSize})";
        }

        private class SegmentGroup
        {
            private readonly List<Segment> segments = new List<Segment>();

            public Segment this[int index] => segments[index];

            public int Count => segments.Count;

            public Segment Add(Segment segment)
            {
                segments.Add(segment);
                segments.Sort((a, b) => a.Offset.CompareTo(b.Offset));
                return segment;
            }

            public int Offset => segments[0].Offset;

            public int AvailableSize
            {
                get
                {
                    var first = segments.First();
                    var last = segments.Last();

                    return (last.Offset + last.AvailableSize) - first.Offset;
                }
            }
        }
    }
}