using System;
using System.Linq;
using System.Text;
using Spike.Scripting.Native;
using Spike.Scripting.Runtime;

namespace Spike.Box
{
    /// <summary>
    /// Represents a class that contains native interop.
    /// </summary>
    public partial class Native
    {

        /// <summary>
        /// Represents an event fired when a script object property has been changed.
        /// </summary>
        /// <param name="instance">The instance of the script object that contains the property.</param>
        /// <param name="changeType">The type of the change.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        /// <param name="oldValue">The old value of the property.</param>
        public static void OnPropertyChange(ScriptObject instance, PropertyChangeType changeType, string propertyName, BoxedValue newValue, BoxedValue oldValue)
        {
            // Ignore identifier property
            if (propertyName == "$i")
                return;

            switch (changeType)
            {
                // Occurs when a new property is assigned to an object, either by
                // using the indexer, property access or array methods.
                case PropertyChangeType.Put:
                {
                    // We need to make sure the new value is marked as observed 
                    // as well, so its members will notify us too.
                    if (newValue.IsStrictlyObject)
                        newValue.Object.Observe();

                    // Send the change through the current scope
                    Channel.Current.SendPropertyChange(PropertyChangeType.Put, instance.Oid, propertyName, newValue);
                    break;
                }

                // Occurs when a property change occurs, by using an assignment
                // operator, or the array indexer.
                case PropertyChangeType.Set:
                {
                    // We need to unmark the old value and ignore it, as we no
                    // longer require observations from that value.
                    if (oldValue.IsStrictlyObject)
                        oldValue.Object.Ignore();

                    // We need to make sure the new value is marked as observed 
                    // as well, so its members will notify us too.
                    if (newValue.IsStrictlyObject)
                        newValue.Object.Observe();

                    // Send the change through the current scope
                    Channel.Current.SendPropertyChange(PropertyChangeType.Set, instance.Oid, propertyName, newValue);
                    break;
                }

                // Occurs when a property was deleted from the object, either by
                // using a 'delete' keyword or the array removal methods.
                case PropertyChangeType.Delete:
                {
                    // We need to unmark the old value and ignore it, as we no
                    // longer require observations from that value.
                    if (oldValue.IsStrictlyObject)
                        oldValue.Object.Ignore();

                    // Send the change through the current scope
                    Channel.Current.SendPropertyChange(PropertyChangeType.Delete, instance.Oid, propertyName, newValue);
                    break;
                }

            }


            //Console.WriteLine("Observe: [{0}] {1} = {2}", changeType.ToString(), propertyName, propertyValue);
        }
    }
}
