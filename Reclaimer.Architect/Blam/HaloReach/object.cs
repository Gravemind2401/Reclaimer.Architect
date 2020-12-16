using Adjutant.Blam.Common;
using Adjutant.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Endian;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Blam.HaloReach
{
    public class @object
    {
        [Offset(0)]
        public short ObjectType { get; set; }

        [Offset(96)]
        public StringId DefaultVariant { get; set; }

        [Offset(100)]
        public TagReference Model { get; set; }

        [Offset(116)]
        public TagReference CrateObject { get; set; }

        [Offset(132)]
        public TagReference CollisionDamage { get; set; }
    }
}
