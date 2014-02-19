using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spike.Scripting.Hosting;
using Spike.Scripting.Runtime;
using Spike.Box.Properties;

namespace Spike.Box
{
    /// <summary>
    /// Represents a scope for a script.
    /// </summary>
    public sealed class ElementScope : Scope
    {
        #region Constructors
        /// <summary>
        /// Constructs a new instance of an <see cref="AppScope"/>.
        /// </summary>
        /// <param name="name">The unique name for the scope.</param>
        /// <param name="parent">The parent scope to attach to the scope.</param>
        /// <param name="context">The execution context that is used to execute scripts.</param>
        internal ElementScope(string name, PageScope parent, ScriptContext context, ScriptObject prototype)
            : base(name, parent, context, prototype)
        {
            
        }
                
        /// <summary>
        /// Creates a new container that servers as an application container for a scope.
        /// </summary>
        /// <param name="prototype">The prototype to use.</param>
        internal ElementScope(ScriptObject prototype)
            : base(prototype)
        {
            
        }
        
        /// <summary>
        /// Creates a child scope.
        /// </summary>
        /// <param name="prototype">The prototype of the scope to create.</param>
        /// <param name="name">The name of the child to assign.</param>
        /// <returns>A new instance of a child scope.</returns>
        protected override Scope CreateChild(string prototype, string name)
        {
            return null;
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the channel that should be used for communication.
        /// </summary>
        public override Channel Channel
        {
            get { return this.Parent.Channel; }
        }
        #endregion

        #region IDisposable Members
        /// <summary>
        /// Occurs when the scope is being disposed.
        /// </summary>
        /// <param name="disposing">Invoke the disposal.</param>
        protected override void OnDispose(bool disposing)
        {
            try
            {
                // Delete from the parent scope
                this.Parent.Delete(this.Name);

                // Call the base
                base.OnDispose(disposing);
            }
            catch (Exception ex)
            {
                Service.Logger.Log(ex);
            }
        }

        #endregion

    }
}
