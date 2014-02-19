using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spike.Box
{
    /// <summary>
    /// Represents a module that can extend native functionality of the box.
    /// </summary>
    interface IModule
    {
        /// <summary>
        /// Gets the name of the module.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Registers the module inside a script context.
        /// </summary>
        /// <param name="context">The context to register the module to.</param>
        void Register(ScriptContext context);

    }
}
