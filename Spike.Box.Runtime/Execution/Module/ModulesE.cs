using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spike.Box.Properties;

namespace Spike.Box
{

    /// <summary>
    /// Module that implements JSON parsing/serializing.
    /// </summary>
    public sealed class JsonModule : IModule
    {
        /// <summary>
        /// Gets the name of the module.
        /// </summary>
        public string Name
        {
            get { return "json"; }
        }

        /// <summary>
        /// Registers the module inside a script context.
        /// </summary>
        /// <param name="context">The context to register the module to.</param>
        public void Register(ScriptContext context)
        {
            context.Eval(Resources.Json);
        }
    }

    /// <summary>
    /// Module that implements LINQ extensions for JavaScript.
    /// </summary>
    public sealed class LinqModule : IModule
    {
        /// <summary>
        /// Gets the name of the module.
        /// </summary>
        public string Name
        {
            get { return "linq"; }
        }

        /// <summary>
        /// Registers the module inside a script context.
        /// </summary>
        /// <param name="context">The context to register the module to.</param>
        public void Register(ScriptContext context)
        {
            context.Eval(Resources.Linq);
        }
    }
}
