using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Reclaimer.Models
{
    internal class BlockPropertiesLocator
    {
        public XmlNode RootNode { get; set; }
        public long BaseAddress { get; set; }
        public IList<Tuple<XmlNode, long>> AdditionalNodes { get; set; }
        public IMetaUpdateReceiver TargetObject { get; set; }
    }
}
