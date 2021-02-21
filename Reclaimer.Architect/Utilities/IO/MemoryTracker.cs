using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Utilities.IO
{
    internal class MemoryTracker
    {
        private readonly List<Span> freeSpace = new List<Span>();

        public void Allocate(int start, int size)
        {
            var request = new Span(start, size);
            var free = freeSpace.FirstOrDefault(s => s.Contains(request));
            if (free == null)
                throw new ArgumentException("Requested span cannot be allocated because it is in use.");

            freeSpace.Remove(free);
            if (free.Start == request.Start && free.End == request.End)
                return; //nothing left

            if (request.Start > free.Start) //leftover space at the beginning
                freeSpace.Add(new Span(free.Start, request.Start - free.Start));

            if (request.End < free.End) //leftover space at the end
                freeSpace.Add(new Span(request.End, free.End - request.End));
        }

        public void Release(int start, int size)
        {
            freeSpace.Add(new Span(start, size));
            Defragment();
        }

        public bool Find(int size, out int start)
        {
            //try to fill the smallest gap available
            var span = freeSpace
                .OrderBy(s => s.Size)
                .FirstOrDefault(s => s.Size >= size);
            start = span?.End - size ?? 0;
            return span != null;
        }

        //insert new free space
        public void Insert(int position, int count)
        {
            var containing = freeSpace.FirstOrDefault(s => s.Contains(position));
            var shifted = freeSpace.Where(s => s.Start > position).ToList();

            //offset everything after the insert
            foreach (var span in shifted)
                span.Start += count;

            //if the space was inserted in the middle of a span then expand it
            if (containing != null)
                containing.Size += count;
            else //else release it as a new span
                Release(position, count);
        }

        private void Defragment()
        {
            freeSpace.Sort((a, b) => a.Start.CompareTo(b.Start));
            for (int i = 0; i < freeSpace.Count - 1;)
            {
                var a = freeSpace[i];
                var b = freeSpace[i + 1];

                if (a.End >= b.Start) //the spans are sequential or overlapping
                {
                    //dont increment - keep merging as long as possible
                    freeSpace.RemoveAt(i + 1);
                    a.Size = b.End - a.Start;

                    //(but break if we just merged the last span)
                    if (i == freeSpace.Count - 1)
                        break;
                }
                else i++; //increment and try the next one
            }
        }

        private class Span
        {
            public int Start { get; set; }
            public int Size { get; set; }
            public int End => Start + Size;

            public Span(int start, int size)
            {
                Start = start;
                Size = size;
            }

            public bool Contains(int position) => position >= Start && position < End;
            public bool Contains(Span other) => Contains(other.Start) && Contains(other.End - 1);

            public override string ToString() => $"{Start} - {End} ({Size})";
        }
    }
}
