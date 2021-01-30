using Reclaimer.Models;
using Reclaimer.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace Reclaimer.Components
{
    public class MissionComponentManager : ComponentManager
    {
        private IEnumerable<NodeType> HandledNodeTypes
        {
            get
            {
                yield return NodeType.Mission;
                yield return NodeType.StartProfiles;
            }
        }

        public MissionComponentManager(ScenarioModel scenario)
            : base(scenario)
        { }

        public override bool HandlesNodeType(NodeType nodeType) => HandledNodeTypes.Any(t => t == nodeType);

        public override IEnumerable<ScenarioListItem> GetListItems(SceneNodeModel treeNode)
        {
            if (treeNode.NodeType == NodeType.StartProfiles)
            {
                var items = new List<ScenarioListItem>();

                var section = scenario.Sections[Section.StartProfiles];
                var nameOffset = OffsetById(section.Node, FieldId.Name);

                using (var reader = scenario.CreateReader())
                {
                    for (int i = 0; i < section.TagBlock.Count; i++)
                    {
                        var baseAddress = section.TagBlock.Pointer.Address + section.BlockSize * i;
                        reader.Seek(baseAddress + nameOffset, SeekOrigin.Begin);

                        var name = reader.ReadNullTerminatedString();
                        items.Add(new ScenarioListItem(name));
                    }
                }

                return items;
            }

            return Enumerable.Empty<ScenarioListItem>();
        }

        internal override BlockPropertiesLocator GetPropertiesLocator(SceneNodeModel treeNode, int itemIndex)
        {
            XmlNode rootNode;
            long baseAddress;
            IMetaUpdateReceiver target;

            if (treeNode.NodeType == NodeType.Mission)
            {
                rootNode = scenario.Sections[Section.Mission].Node;
                baseAddress = scenario.RootAddress;
                target = null;
            }
            else if (treeNode.NodeType == NodeType.StartProfiles && itemIndex >= 0)
            {
                var section = scenario.Sections[Section.StartProfiles];

                rootNode = section.Node;
                baseAddress = section.TagBlock.Pointer.Address
                    + itemIndex * section.BlockSize;

                target = scenario.Items[itemIndex];
            }
            else return null;

            return new BlockPropertiesLocator
            {
                RootNode = rootNode,
                BaseAddress = baseAddress,
                TargetObject = target
            };
        }
    }
}
