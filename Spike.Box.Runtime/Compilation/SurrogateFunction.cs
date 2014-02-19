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
    /// Represents a function member of the proxy.
    /// </summary>
    internal class SurrogateFunction : SurrogateMember
    {
        /// <summary>
        /// Constructs a new proxy member.
        /// </summary>
        /// <param name="name">The name of the member.</param>
        public SurrogateFunction(string name)
            : base(name)
        {

        }

    }

    #region NamedFunctionObject
    /// <summary>
    /// Represents a named function object.
    /// </summary>
    public struct NamedFunctionObject
    {
        /// <summary>
        /// Constructs a new instance of a named function object.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <param name="func">The function object.</param>
        public NamedFunctionObject(string name, FunctionObject func)
        {
            this.Name = name;
            this.Func = func;
        }

        /// <summary>
        /// Gets the name of the function.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Gets the function object.
        /// </summary>
        public readonly FunctionObject Func;
    }
    #endregion
}
