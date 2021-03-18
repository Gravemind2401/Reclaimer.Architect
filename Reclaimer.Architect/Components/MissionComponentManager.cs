using Reclaimer.Models;
using Reclaimer.Resources;
using Reclaimer.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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

        public override bool SupportsObjectOperation(ObjectOperation operation, NodeType nodeType) => nodeType == NodeType.StartProfiles;

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
                        if (string.IsNullOrEmpty(name))
                            name = "<none>";

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

        public override bool ExecuteObjectOperation(SceneNodeModel treeNode, ObjectOperation operation, int itemIndex)
        {
            var blockRef = scenario.Sections[Section.StartProfiles];
            var blockEditor = scenario.MetadataStream.GetBlockEditor(blockRef.TagBlock.Pointer.Address);

            switch (operation)
            {
                case ObjectOperation.Add:
                    blockEditor.Add();
                    break;

                case ObjectOperation.Remove:
                    if (itemIndex < 0 || itemIndex >= blockRef.TagBlock.Count)
                        return false;

                    blockEditor.Remove(itemIndex);
                    break;

                case ObjectOperation.Copy:
                    if (itemIndex < 0 || itemIndex >= blockRef.TagBlock.Count)
                        return false;

                    blockEditor.Copy(itemIndex, itemIndex + 1);
                    break;
            }

            blockEditor.UpdateBlockReference(blockRef);
            return true;
        }
    }
}
