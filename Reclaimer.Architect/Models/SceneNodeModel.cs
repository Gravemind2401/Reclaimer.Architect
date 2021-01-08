using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Models
{
    public class SceneNodeModel : TreeItemModel
    {
        private NodeType nodeType;
        public NodeType NodeType
        {
            get { return nodeType; }
            set { SetProperty(ref nodeType, value); }
        }

        private int iconType;
        public int IconType
        {
            get { return iconType; }
            set { SetProperty(ref iconType, value); }
        }

        public SceneNodeModel()
            : base()
        {
            NodeType = NodeType.None;
        }

        public SceneNodeModel(string header, NodeType type)
            : base(header)
        {
            NodeType = type;
        }
    }
}
