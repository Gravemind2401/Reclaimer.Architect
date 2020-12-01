using Adjutant.Blam.Common;
using Adjutant.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Endian;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Blam.Halo3
{
    public class @object
    {
        [Offset(0)]
        public short ObjectType { get; set; }

        [Offset(48)]
        public StringId DefaultVariant { get; set; }

        [Offset(52)]
        public TagReference Model { get; set; }

        [Offset(68)]
        public TagReference CrateObject { get; set; }

        [Offset(84)]
        public TagReference CollisionDamage { get; set; }
    }
}
