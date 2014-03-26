using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spike.Scripting.Hosting;
using Spike.Scripting.Runtime;
using Spike.Box.Properties;
using System.Threading.Tasks;

namespace Spike.Box
{
    /// <summary>
    /// Represents an abstract scope.
    /// </summary>
    public abstract class Scope : BaseObject
    {
        #region Constructors
        /// <summary>
        /// Gets the collection of children associated with this scope
        /// </summary>
        private readonly ConcurrentDictionary<string, Scope> Registry =
            new ConcurrentDictionary<string, Scope>();

        /// <summary>
        /// Gets the underlying script context used to run scripts.
        /// </summary>
        protected readonly ScriptContext Context;

        /// <summary>
        /// Constructs a new instance of an <see cref="ScopeBase"/>.
        /// </summary>
        /// <param name="name">The unique name for the scope.</param>
        /// <param name="parent">The parent scope to attach to the scope.</param>
        /// <param name="context">The execution context that is used to execute scripts.</param>
        internal Scope(string name, Scope parent, ScriptContext context, ScriptObject prototype)
            : base(prototype)
        {
            this.Name = name;
            this.Parent = parent;
            this.Context = context;

            // Map the scope
            ScriptMap.Map(this);
        }

                /// <summary>
        /// Creates a new container that servers as an application container for a scope.
        /// </summary>
        /// <param name="prototype">The prototype to use.</param>
        internal Scope(ScriptObject prototype)
            : base(prototype)
        {
            throw new InvalidOperationException("A scope cannot be instanciated from javascript code.");
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gest the unique name of the scope.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }


        /// <summary>
        /// Gets the parent scope.
        /// </summary>
        public Scope Parent
        {
            get;
            private set;
        }
        #endregion

        #region Abstract Members
        /// <summary>
        /// Creates a child scope.
        /// </summary>
        /// <param name="name">The name of the child to assign.</param>
        /// <param name="prototype">The prototype of the scope to create.</param>
        /// <returns>A new instance of a child scope.</returns>
        protected abstract Scope CreateChild(string prototype, string name);

        /// <summary>
        /// Gets the channel that should be used for communication.
        /// </summary>
        public abstract Channel Channel
        {
            get;
        }
        #endregion

        #region Public Members (Children)

        /// <summary>
        /// Attempts to get or create a new child within this scope.
        /// </summary>
        /// <param name="name">The name of the child to get or create.</param>
        /// <typeparam name="T">The type of the child to get or create.</typeparam>
        /// <returns>The retrieved or created child.</returns>
        public T Within<T>(string name)
            where T : Scope
        {
            Scope scope;
            if (!this.TryGetChild(name, out scope))
                this.TryCreateChild(null, name, out scope);

            return (T)scope;
        }

        /// <summary>
        /// Attempts to get or create a new child within this scope.
        /// </summary>
        /// <param name="prototype">The prototype of the scope to create.</param>
        /// <param name="name">The name of the child to get or create.</param>
        /// <typeparam name="T">The type of the child to get or create.</typeparam>
        /// <returns>The retrieved or created child.</returns>
        public T Within<T>(string prototype, string name)
            where T : Scope
        {
            Scope scope;
            if (!this.TryGetChild(name, out scope))
                this.TryCreateChild(prototype, name, out scope);

            return (T)scope;
        }

        /// <summary>
        /// Attempts to create a new child within this scope.
        /// </summary>
        /// <param name="name">The name of the child to create.</param>
        /// <param name="prototype">The prototype of the scope to create.</param>
        /// <param name="child">The created child instance.</param>
        /// <returns>Whether the child was created or not.</returns>
        public bool TryCreateChild(string prototype, string name, out Scope child)
        {
            // Create a new instance
            var instance = this.CreateChild(prototype, name);
            child = null;

            // Check if we create a child
            if (instance == null)
                return false;

            // Attempt to add a new instance
            if (this.Registry.TryAdd(name, instance))
            {
                // Attach it to the parent as a property
                this.Put(name, instance);

                // Replace the output
                child = instance;
                return true;
            }

            // Already there, fail
            return false;
        }

        /// <summary>
        /// Attempts to delete a child from this scope.
        /// </summary>
        /// <param name="name">The name of the child to delete.</param>
        /// <returns>Whether the child was deleted or not.</returns>
        public bool TryDeleteChild(string name)
        {
            // Attempt to remove and then dispose the child
            Scope child;
            if (!this.Registry.TryRemove(name, out child))
                return false;

            // Dispose the child
            child.Dispose();
            return true;
        }

        /// <summary>
        /// Attempts to retrieve a child from this scope.
        /// </summary>
        /// <param name="name">The name of the child to retrieve.</param>
        /// <param name="child">The retrieved child instance.</param>
        /// <returns>Whether the child was retrieved or not.</returns>
        public bool TryGetChild(string name, out Scope child)
        {
            // Simply get from the table
            return this.Registry.TryGetValue(name, out child);
        }

        /// <summary>
        /// Gets all the children within this scope.
        /// </summary>
        /// <returns>The children residing within the scope.</returns>
        public IEnumerable<Scope> GetChildren()
        {
            return this.Registry.Values;
        }

        #endregion

        #region Public Members (Scripting)
        /// <summary>
        /// Includes a script in the scope.
        /// </summary>
        /// <param name="script">The script to include.</param>
        internal Surrogate Include(MetaScript script)
        {
            // Include the server script
            return this.Context.Include(script);
        }

        /// <summary>
        /// Invokes a method on this scope, with the provided arguments.
        /// </summary>
        /// <param name="methodName">The method to invoke.</param>
        /// <param name="args">The arguments to pass to this method.</param>
        /// <param name="channel">The channel</param>
        /// <returns>The result as a string or a JSON string.</returns>
        public string CallMethod(string methodName, IList<string> args, Channel channel)
        {
            // Invoke on the script context, natively
            return channel.Dispatch<string>(() => Native.Call(this, methodName, args));
        }

        /// <summary>
        /// Call a property setter.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="propertyValue">The value of the property.</param>
        /// <param name="channel">The channel</param>
        public void CallSetter(string propertyName, BoxedValue propertyValue, Channel channel)
        {
            // Invoke on the context
            channel.Dispatch(() =>
            {
                // Get the setter 
                var setter = this.Get(propertyName);
                if (setter.IsFunction)
                {
                    // Call the setter function with the specified value
                    setter.Func.Call(this, propertyValue);
                }
            });
        }
        #endregion

        #region Private Members
        /// <summary>
        /// Gets the property of the scope object attached to the script.
        /// </summary>
        /// <typeparam name="T">The type of the object attached.</typeparam>
        /// <param name="name">The name of the object.</param>
        /// <returns>The retrieved object.</returns>
        protected T Get<T>(string name)
            where T: ScriptObject
        {
            return this.Context.Get<T>(this, name);
        }

        #endregion

        #region IDisposable Members
        /// <summary>
        /// Occurs when the scope is being disposed.
        /// </summary>
        /// <param name="disposing">Invoke the disposal.</param>
        protected override void OnDispose(bool disposing)
        {
            // Detach from the parent
            this.Parent = null;

            // We do not need to call the base, as we remove from
            // the cache without any special events.
            //base.OnDispose(disposing);
        }

        #endregion

    }



}
