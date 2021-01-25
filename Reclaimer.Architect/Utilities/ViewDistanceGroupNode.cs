using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Model.Scene;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Utilities
{
    public sealed class ViewDistanceGroupNode : GroupNode
    {
        private readonly Element3D element;

        private bool isDormant;
        private BoundingBox originalBounds;

        public ViewDistanceGroupNode(Element3D element)
        {
            this.element = element;
        }

        private void ScanAncestors()
        {
            //if one of the ancestors is already a ViewDistanceGroupNode
            //then this node should act like a regular GroupNode
            //so it does not interfere with the ancestor

            isDormant = false;
            var ancestor = Parent;
            while (ancestor != null)
            {
                if (ancestor is ViewDistanceGroupNode)
                {
                    isDormant = true;
                    return;
                }
                else ancestor = ancestor.Parent;
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            isDormant = true;
            //ScanAncestors();
            //originalBounds = element.GetTotalBounds(true);
        }

        protected override bool CanRender(RenderContext context)
        {
            var baseValue = base.CanRender(context);
            if (isDormant || !baseValue)
                return baseValue;

            var bounds = originalBounds.Transform(element.Transform.ToMatrix());
            var camPos = context.Camera.Position;
            if (bounds.Contains(ref camPos) == ContainmentType.Contains)
                return true;

            //var length = (bounds.Center - camPos).Length();
            //if (length < 5)
            //    return true;

            var screenBounds = bounds.Project(context);
            return screenBounds.Width > 3 && screenBounds.Height > 3;
        }
    }
}
