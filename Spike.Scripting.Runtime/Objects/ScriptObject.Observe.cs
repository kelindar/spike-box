using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Spike.Scripting.Runtime
{
    /// <summary>
    /// Represents a script object.
    /// </summary>
    public partial class ScriptObject
    {
        #region Public Properties
        private readonly int Key;
        private ScriptObjectFlag Flags;

        /// <summary>
        /// An event that occurs when a property of a script object has changed.
        /// </summary>
        public static event ScriptObjectChangedEventHandler PropertyChange;


        /// <summary>
        /// An event that occurs when an object is being observed.
        /// </summary>
        public static event Action<ScriptObject> Observed;


        /// <summary>
        /// An event that occurs when an object is being ignored.
        /// </summary>
        public static event Action<ScriptObject> Ignored;


        /// <summary>
        /// Gets or sets whether this object is observed or not.
        /// </summary>
        public bool IsObserved
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get 
            {
                return this.Flags.HasFlag(ScriptObjectFlag.Observe) 
                    && !(this is RegExpObject); 
            }
        }       
        /// <summary>
        /// Gets the object identification number.
        /// </summary>
        public int Oid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return this.Key; }
        }
        #endregion

        #region Public Members (OnPropertyChange)
        /// <summary>
        /// Occurs when a property changes.
        /// </summary>
        /// <param name="type">Type of the property.</param>
        /// <param name="name">Name of the property.</param>
        /// <param name="newValue">Value of the property.</param>
        /// <param name="oldValue">The old value of the property.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnPropertyChange(PropertyChangeType type, string name, object newValue, BoxedValue oldValue)
        {
            // Invoke the changed event
            if (ScriptObject.PropertyChange != null)
                ScriptObject.PropertyChange(this, type, name, BoxedValue.Box(newValue), oldValue);
        }

        /// <summary>
        /// Occurs when a property changes.
        /// </summary>
        /// <param name="type">Type of the property.</param>
        /// <param name="name">Name of the property.</param>
        /// <param name="newValue">Value of the property.</param>
        /// <param name="oldValue">The old value of the property.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnPropertyChange(PropertyChangeType type, string name, BoxedValue newValue, BoxedValue oldValue)
        {
            // Invoke the changed event
            if (ScriptObject.PropertyChange != null)
                ScriptObject.PropertyChange(this, type, name, newValue, oldValue);
        }

        /// <summary>
        /// Occurs when a property changes.
        /// </summary>
        /// <param name="type">Type of the property.</param>
        /// <param name="name">Name of the property.</param>
        /// <param name="newValue">Value of the property.</param>
        /// <param name="oldValue">The old value of the property.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnPropertyChange(PropertyChangeType type, string name, double newValue, BoxedValue oldValue)
        {
            // Invoke the changed event
            if (ScriptObject.PropertyChange != null)
                ScriptObject.PropertyChange(this, type, name, BoxedValue.Box(newValue), oldValue);
        }
        #endregion

        #region Static Members (InvokePropertyChange)
        /// <summary>
        /// Occurs when a property changes.
        /// </summary>
        /// <param name="type">Type of the property.</param>
        /// <param name="name">Name of the property.</param>
        /// <param name="newValue">Value of the property.</param>
        /// <param name="oldValue">The old value of the property.</param>
        public static void InvokePropertyChange(ScriptObject instance, PropertyChangeType type, string name, object newValue, BoxedValue oldValue)
        {
            // Invoke the changed event
            if (ScriptObject.PropertyChange != null)
                ScriptObject.PropertyChange(instance, type, name, BoxedValue.Box(newValue), oldValue);
        }

        /// <summary>
        /// Occurs when a property changes.
        /// </summary>
        /// <param name="type">Type of the property.</param>
        /// <param name="name">Name of the property.</param>
        /// <param name="newValue">Value of the property.</param>
        /// <param name="oldValue">The old value of the property.</param>
        public static void InvokePropertyChange(ScriptObject instance, PropertyChangeType type, string name, BoxedValue newValue, BoxedValue oldValue)
        {
            // Invoke the changed event
            if (ScriptObject.PropertyChange != null)
                ScriptObject.PropertyChange(instance, type, name, newValue, oldValue);
        }

        /// <summary>
        /// Occurs when a property changes.
        /// </summary>
        /// <param name="type">Type of the property.</param>
        /// <param name="name">Name of the property.</param>
        /// <param name="newValue">Value of the property.</param>
        /// <param name="oldValue">The old value of the property.</param>
        public static void InvokePropertyChange(ScriptObject instance, PropertyChangeType type, string name, double newValue, BoxedValue oldValue)
        {
            // Invoke the changed event
            if (ScriptObject.PropertyChange != null)
                ScriptObject.PropertyChange(instance, type, name, BoxedValue.Box(newValue), oldValue);
        }

        #endregion

        #region Public Members (Observe)
        /// <summary>
        /// Marks the <see cref="ScriptObject"/> for observation and property monitoring.
        /// </summary>
        public void Observe()
        {
            // If it's already observed, do not do anything
            if (this.Flags.HasFlag(ScriptObjectFlag.Observe))
                return;

            // Mark the object as observed
            this.Flags |= ScriptObjectFlag.Observe;

            // Put the key as the $i property
            this.Put("$i", this.Key);

            // Invoke observed event
            if (ScriptObject.Observed != null)
                ScriptObject.Observed(this);

            // Observe it as an associative array or as an object
            this.ObserveChildren();
        }

        /// <summary>
        /// Mark this as observed and each child should be marked as observer as well, ensuring
        /// that the entire object tree becomes observable and any property change on the tree
        /// is tracked and an event raised when a change occurs.
        /// </summary>
        protected virtual void ObserveChildren()
        {
            foreach (var kvp in this.PropertySchema.IndexMap)
            {
                // Only properties with a value
                var property = this.Properties[kvp.Value];
                if (!property.HasValue)
                    continue;

                // Only objects, no functions
                var boxedValue = property.Value;
                if (!boxedValue.IsObject || boxedValue.IsFunction)
                    continue;

                // Debug
                //Console.WriteLine("Observe: " + kvp.Key);

                // Observe the child object
                boxedValue.Object.Observe();
            }
        }
        #endregion

        #region Public Members (Ignore)

        /// <summary>
        /// Marks the observed <see cref="ScriptObject"/> and property monitoring.
        /// </summary>
        public void Ignore()
        {
            // If it's not observed, do not do anything
            if (!this.Flags.HasFlag(ScriptObjectFlag.Observe))
                return;

            // Unmark the object as observed
            this.Flags &= ~ScriptObjectFlag.Observe;

            // Invoke ignored event
            if (ScriptObject.Ignored != null)
                ScriptObject.Ignored(this);

            // Observe it as an associative array or as an object
            this.IgnoreChildren();
        }

        /// <summary>
        /// Marks each observed child as non-observed, removing recursively the flag.
        /// </summary>
        protected virtual void IgnoreChildren()
        {
            foreach (var kvp in this.PropertySchema.IndexMap)
            {
                // Only properties with a value
                var property = this.Properties[kvp.Value];
                if (!property.HasValue)
                    continue;

                // Only objects, no functions
                var boxedValue = property.Value;
                if (!boxedValue.IsObject || boxedValue.IsFunction)
                    continue;

                // Debug
                //Console.WriteLine("Ignore: " + kvp.Key);

                // Ignore the child object
                boxedValue.Object.Ignore();
            }
        }
        #endregion

    }

    /// <summary>
    /// Represents an event fired when a script object property has been changed.
    /// </summary>
    /// <param name="instance">The instance of the script object that contains the property.</param>
    /// <param name="changeType">The type of the change.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="newValue">The new value of the property.</param>
    /// <param name="oldValue">The old value of the property.</param>
    public delegate void ScriptObjectChangedEventHandler(ScriptObject instance, PropertyChangeType changeType, string propertyName, BoxedValue newValue, BoxedValue oldValue);

    /// <summary>
    /// Represents a flags assigned on a script object.
    /// </summary>
    [Flags]
    public enum ScriptObjectFlag : byte
    {
        /// <summary>
        /// Represents a script object without any special flags.
        /// </summary>
        None = 0,

        /// <summary>
        /// Represents an observed script object.
        /// </summary>
        Observe = 1
    }

    /// <summary>
    /// Type of property change.
    /// </summary>
    public enum PropertyChangeType : byte
    {
        /// <summary>
        /// A new property was assigned.
        /// </summary>
        Put,

        /// <summary>
        /// A property value has changed.
        /// </summary>
        Set,

        /// <summary>
        /// A property was deleted.
        /// </summary>
        Delete
    }

}
