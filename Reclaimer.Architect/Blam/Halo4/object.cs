using Adjutant.Blam.Common;
using Adjutant.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Endian;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Blam.Halo4
{
    public class @object
    {
        [Offset(0)]
        public short ObjectType { get; set; }

        [Offset(132)]
        public StringId DefaultVariant { get; set; }

        [Offset(136)]
        public TagReference Model { get; set; }

        [Offset(152)]
        public TagReference CrateObject { get; set; }

        [Offset(168)]
        public TagReference CollisionDamage { get; set; }
    }
}
