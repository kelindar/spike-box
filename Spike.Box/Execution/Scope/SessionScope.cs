using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spike.Scripting.Hosting;
using Spike.Scripting.Runtime;
using Spike.Box.Properties;
using Spike.Network;
using Spike.Collections;

namespace Spike.Box
{
    /// <summary>
    /// Represents an application session scope.
    /// </summary>
    public sealed class SessionScope : Scope
    {
        #region Constructors
        private readonly Channel SessionChannel;

        /// <summary>
        /// Constructs a new instance of an <see cref="AppScope"/>.
        /// </summary>
        /// <param name="name">The unique name for the scope.</param>
        /// <param name="parent">The parent scope to attach to the scope.</param>
        /// <param name="context">The execution context that is used to execute scripts.</param>
        internal SessionScope(string name, AppScope parent, ScriptContext context)
            : base(name, parent, context, context.GetPrototype("Session"))
        {
            // Create a new channel linked to the session
            this.SessionChannel = new Channel(context);
        }

        /// <summary>
        /// Creates a child scope.
        /// </summary>
        /// <param name="prototype">The prototype of the scope to create.</param>
        /// <param name="name">The name of the child to assign.</param>
        /// <returns>A new instance of a child scope.</returns>
        protected override Scope CreateChild(string prototype, string name)
        {
            // Create a new page scope
            try
            {
                var protoref = this.Context.GetPrototype(prototype);
                var instance = new PageScope(name, this, this.Context, protoref);

                // Call the constructor
                instance.Prototype.Get("constructor").Func.Call(instance);

                // Attach the session to this & return
                instance.Put("session", this);
                return instance;
            }
            catch (InvalidCastException ex)
            {
                if (!ex.Message.Contains("Undefined"))
                    throw ex;

                throw new ArgumentException("Unable to create or retrieve the object of type " + prototype + ".");
            }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the class name for building the javascript name.
        /// </summary>
        public override string ClassName
        {
            get { return "Session"; }
        }

        /// <summary>
        /// Gets the channel that should be used for communication.
        /// </summary>
        public override Channel Channel
        {
            get { return this.SessionChannel; }
        }
        #endregion


        #region IDisposable Members
        /// <summary>
        /// Occurs when the session object is disposing.
        /// </summary>
        protected override void OnDispose(bool disposing)
        {
            try
            {
                // Delete from the parent scope
                this.Parent.Delete(this.Name);

                // Delete the session info from each sub page
                foreach (var property in this.Properties)
                {
                    if (property.HasValue && property.Value.IsObject)
                    {
                        // Make sure it's a page scope
                        var pageScope = property.Value.Object as PageScope;
                        if (pageScope == null)
                            continue;

                        try
                        {
                            // Detatch the parent and delete session value
                            pageScope.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Service.Logger.Log(ex);
                        }
                    }
                }

                // Dispose the underlying channel
                this.SessionChannel.Dispose();

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
