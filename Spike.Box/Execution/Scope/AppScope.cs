using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spike.Scripting.Hosting;
using Spike.Box.Properties;

namespace Spike.Box
{
    /// <summary>
    /// Represents a global application scope.
    /// </summary>
    public sealed class AppScope : Scope
    {
        /// <summary>
        /// Constructs a new instance of an <see cref="AppScope"/>.
        /// </summary>
        /// <param name="application">The owner application.</param>
        /// <param name="context">The scripting context for this scope.</param>
        internal AppScope(App application, ScriptContext context)
            : base("app" + application.Key, null, context, context.GetPrototype("Application"))
        {
            context.AttachGlobal("app" + application.Key, this);
        }

        /// <summary>
        /// Creates a child scope.
        /// </summary>
        /// <param name="prototype">The prototype of the scope to create.</param>
        /// <param name="name">The name of the child to assign.</param>
        /// <returns>A new instance of a child scope.</returns>
        protected override Scope CreateChild(string prototype, string name)
        {
            return new SessionScope(name, this, this.Context);
        }

        /// <summary>
        /// Gets the channel that should be used for communication.
        /// </summary>
        public override Channel Channel
        {
            get { return Channel.Current; }
        }
    }

}
