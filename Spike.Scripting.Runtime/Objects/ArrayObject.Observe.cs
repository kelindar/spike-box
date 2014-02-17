using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spike.Scripting.Runtime
{
    /// <summary>
    /// Represent an array object.
    /// </summary>
    public sealed partial class ArrayObject
    {

        /// <summary>
        /// Mark this as observed and each child should be marked as observer as well, ensuring
        /// that the entire object tree becomes observable and any property change on the tree
        /// is tracked and an event raised when a change occurs.
        /// </summary>
        protected sealed override void ObserveChildren()
        {
            var length = this.Length;
            for (int i = 0; i < length; ++i)
            {
                // Get the child at a particular index.
                var child = this.Get(i);

                // Only observe a child if it's an object, exclude functions as they do not have
                // children to observe.
                if (child.IsStrictlyObject)
                    child.Object.Observe();
            }
        }

        /// <summary>
        /// Marks each observed child as non-observed, removing recursively the flag.
        /// </summary>
        protected sealed override void IgnoreChildren()
        {
            var length = this.Length;
            for (int i = 0; i < length; ++i)
            {
                // Get the child at a particular index.
                var child = this.Get(i);

                // Only observe a child if it's an object, exclude functions as they do not have
                // children to observe.
                if (child.IsStrictlyObject)
                    child.Object.Ignore();
            }
        }
    }

}
