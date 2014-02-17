using System;
using System.Linq;
using Spike.Scripting;
using Spike.Scripting.Runtime;
using Spike.Scripting.Compiler;
using Spike.Scripting.Hosting;
using System.Collections.Generic;
using System.Text;

namespace Spike.Box
{
    /// <summary>
    /// Represents a function call for the proxy.
    /// </summary>
    internal abstract class SurrogateMember
    {
        /// <summary>
        /// Constructs a new proxy member.
        /// </summary>
        /// <param name="name">The name of the member.</param>
        public SurrogateMember(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Gets the name of the function call.
        /// </summary>
        public virtual string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the access modifier of the member.
        /// </summary>
        public virtual SurrogateMemberModifier Modifier
        {
            get
            {
                return this.Name.IsPrivateName()
                    ? SurrogateMemberModifier.Private
                    : SurrogateMemberModifier.Public;
            }
        }
    }

    /// <summary>
    /// Represents the type of the surrogate to generate.
    /// </summary>
    public enum SurrogateType
    {
        /// <summary>
        /// A surrogate controller for the view.
        /// </summary>
        ViewSurrogate,

        /// <summary>
        /// A surrogate controller for the directive.
        /// </summary>
        ElementSurrogate
    }

}
