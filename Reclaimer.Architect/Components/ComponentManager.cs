using Reclaimer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Media = System.Windows.Media;
using Media3D = System.Windows.Media.Media3D;
using Helix = HelixToolkit.Wpf.SharpDX;

namespace Reclaimer.Components
{
    public abstract class ComponentManager
    {
        protected readonly ScenarioModel scenario;

        public ComponentManager(ScenarioModel scenario)
        {
            this.scenario = scenario;
        }

        //true if this component deal with this type of scenario node
        public virtual bool HandlesNodeType(NodeType nodeType) => false;

        //if it can be loaded asynchronously do it here
        public virtual Task InitializeResourcesAsync() => Task.CompletedTask;

        //anything that must be synchronous such as building elements can be done here
        public virtual void InitializeElements() { }

        //provide any elements that shoule be added to the scene
        public virtual IEnumerable<Helix.Element3D> GetElements() => Enumerable.Empty<Helix.Element3D>();

        //provide any nodes that should be added to the scene tree
        public virtual IEnumerable<TreeItemModel> GetSceneNodes() => Enumerable.Empty<TreeItemModel>();

        //called for any type of node, even null node
        public virtual void OnSelectedTreeNodeChanged(SceneNodeModel newNode) { }

        //only called if HandlesNodeType, can return null if item is not geometry
        public virtual Helix.Element3D GetElement(SceneNodeModel treeNode, int itemIndex)
        {
            throw new NotImplementedException();
        }

        //only called if HandlesNodeType 
        public virtual SharpDX.BoundingBox GetObjectBounds(SceneNodeModel treeNode, int itemIndex)
        {
            throw new NotImplementedException();
        }

        //only called if HandlesNodeType 
        public virtual int GetElementIndex(SceneNodeModel treeNode, Helix.Element3D element)
        {
            throw new NotImplementedException();
        }

        //only called if HandlesNodeType 
        internal virtual BlockPropertiesLocator GetPropertiesLocator(SceneNodeModel treeNode, int itemIndex)
        {
            throw new NotImplementedException();
        }
    }
}
