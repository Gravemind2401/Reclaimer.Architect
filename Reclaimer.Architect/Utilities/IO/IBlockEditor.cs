using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Utilities.IO
{
    public interface IBlockEditor
    {
        void Add();
        void Remove(int index);
        void Insert(int index);
        void Copy(int sourceIndex, int destIndex);
        void Resize(int newCount);
    }
}
