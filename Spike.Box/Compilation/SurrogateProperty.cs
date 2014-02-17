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
    /// Represents a property member of the proxy.
    /// </summary>
    internal class SurrogateProperty : SurrogateMember
    {
        /// <summary>
        /// Constructs a new proxy member.
        /// </summary>
        /// <param name="name">The name of the member.</param>
        public SurrogateProperty(ScriptPropertyInfo info)
            : base(info.Name)
        {
            this.HasGetter = info.HasGetter;
            this.HasSetter = info.HasSetter;
        }

        /// <summary>
        /// Gets whether the property contains a getter.
        /// </summary>
        public bool HasGetter
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets whether the property contains a setter.
        /// </summary>
        public bool HasSetter
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the access modifier of the member.
        /// </summary>
        public override SurrogateMemberModifier Modifier
        {
            get
            {
                if (!this.HasSetter && !this.HasGetter)
                    return SurrogateMemberModifier.Private;
                return base.Modifier;
            }
        }
    }


    #region ScriptPropertyMeta
    /// <summary>
    /// Represents a collection of script properties.
    /// </summary>
    public sealed class ScriptPropertiesInfo : List<ScriptPropertyInfo>
    {

    }

    /// <summary>
    /// Represents a meta information about the script property
    /// </summary>
    public sealed class ScriptPropertyInfo
    {
        /// <summary>
        /// Constructs a new instance of a property meta object.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        public ScriptPropertyInfo(string name, bool hasGetter, bool hasSetter)
        {
            this.Name = name;
            this.HasGetter = hasGetter;
            this.HasSetter = hasSetter;
        }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Gets whether the property contains a getter.
        /// </summary>
        public readonly bool HasGetter;

        /// <summary>
        /// Gets whether the property contains a setter.
        /// </summary>
        public readonly bool HasSetter;

    }
    #endregion
}
