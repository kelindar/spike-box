using System;
using System.Linq;
using Spike.Network;
using Spike.Scripting;
using Spike.Scripting.Runtime;
using Spike.Scripting.Compiler;
using Spike.Scripting.Hosting;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.IO;
using Spike.Scripting.Native;

namespace Spike.Box
{
    /// <summary>
    /// Represents a class that wraps a CLR reference type.
    /// </summary>
    public class ReferenceObject<T> : BaseObject, IReference<T>
        where T : class
    {
        #region Constructors
        /// <summary>
        /// Gets the underlying channel.
        /// </summary>
        private readonly WeakReference<T> Ref;

        /// <summary>
        /// Creates a new application object that servers as a proxy for
        /// using .Net framework functionality.
        /// </summary>
        /// <param name="prototype">The prototype to fetch the environment from.</param>
        /// <param name="target">The target to wrap.</param>
        public ReferenceObject(ScriptObject prototype, T target)
            : base(prototype)
        {
            // Set a weakly referenced client.
            this.Ref = new WeakReference<T>(target);
        }

        /// <summary>
        /// Creates a new application object that servers as a proxy for
        /// using .Net framework functionality.
        /// </summary>
        /// <param name="environment">The scripting environment to use.</param>
        /// <param name="target">The target to wrap.</param>
        public ReferenceObject(Spike.Scripting.Runtime.Environment environment, T target)
            : this(environment.Prototypes.Object, target)
        {

        }

        /// <summary>
        /// Creates a new application object that servers as a proxy for
        /// using .Net framework functionality.
        /// </summary>
        /// <param name="context">The scripting context to use.</param>
        /// <param name="target">The target to wrap.</param>
        public ReferenceObject(ScriptContext context, T target)
            : this(context.Environment.Prototypes.Object, target)
        {

        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets an indication whether the object referenced by the current System.WeakReference
        /// object has been garbage collected.
        /// </summary>
        public bool IsAlive
        {
            get { return this.Ref.IsAlive; }
        }


        /// <summary>
        /// Gets the object (the target) referenced by the current System.WeakReference
        /// object.
        /// </summary>
        public T Target
        {
            get { return this.Ref.Target; }
        }


        /// <summary>
        /// Gets the class name for building the javascript name.
        /// </summary>
        public override string ClassName
        {
            get { return "Reference"; }
        }
        #endregion

    }

    /// <summary>
    /// Represents a contract that defines a reference object.
    /// </summary>
    /// <typeparam name="T">The type of the object to reference.</typeparam>
    public interface IReference<T>
        where T : class
    {
        /// <summary>
        /// Gets an indication whether the object referenced by the current System.WeakReference
        /// object has been garbage collected.
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// Gets the object (the target) referenced by the current System.WeakReference
        /// object.
        /// </summary>
        T Target { get; }
    }
}


