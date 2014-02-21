using System;
using System.Collections.Generic;
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
        #region Object: defineProperty
        internal static void DefineProperty(FunctionObject hostCtx, ScriptObject hostFunc, ScriptObject obj, string propertyName, ScriptObject info)
        {
            // Get the getter and the setter
            var getter = info.Has("get") ? info.GetT<FunctionObject>("get") : null;
            var setter = info.Has("set") ? info.GetT<FunctionObject>("set") : null;
            var shared = !propertyName.IsPrivateName();

            // If there's no getters or setters, there's no property
            if (getter == null && setter == null)
                return;

            // Attach an array of properties to this object
            if (!obj.Has("properties"))
                obj.Put("properties", new ScriptPropertiesInfo());

            // Push the property name to the array
            obj.GetT<ScriptPropertiesInfo>("properties")
               .Add(new ScriptPropertyInfo(propertyName, getter != null, setter != null));

            // Put the native function handler to it
            obj.Put(propertyName, Utils.CreateFunction<Func<FunctionObject, ScriptObject, BoxedValue, BoxedValue>>(obj.Env, 1, (context, instance, value) => 
            {
                // If the value is not defined, this is agetter.
                if (value.IsUndefined)
                {
                    // Call a getter if there's one.
                    if (getter != null)
                    {
                        // Call the getter
                        var result = getter.Call(instance);

                        // Check if the result is an object and not a function
                        if (result.IsObject && !result.IsFunction)
                        {
                            // Make sure the result is marked as observable
                            result.Object.Observe();
                        }

                        // Return the result now we're done with it
                        return result;
                    }
                    return Undefined.Boxed;
                }
                else
                {
                    if (setter != null)
                    {
                        // Call the setter
                        setter.Call(instance, value);

                        // Dispatch the event, if we have a public getter
                        if (getter != null && shared)
                            Channel.Current.DispatchProperty(BoxedValue.Box(instance), propertyName, getter.Call(instance));
                    }

                    // A setter does not return anything
                    return Undefined.Boxed;
                }
            }));
        }
        #endregion

        #region Array: forEach, removeAll, every, swap

        /// <summary>
        /// Executes a foreach loop on every element of the array.
        /// </summary>
        /// <param name="callback">The callback to execute for each item in the array.</param>
        internal static void Array_ForEach(FunctionObject ctx, ScriptObject instance, FunctionObject callback, BoxedValue thisArg)
        {
            // Make sure the instance is an array
            var array = instance as ArrayObject;
            if(array == null)
                return;

            // Get this argument
            var target = thisArg.IsUndefined 
                ? instance
                : thisArg.Object;
            
            for(int i=0; i< array.Length; ++i)
            {
                // Call the callback 
                callback.Call(target, array.Get(i), BoxedValue.Box(i), array);
            }
        }

        /// <summary>
        /// Removes all elements in the array that match the predicate.
        /// </summary>
        /// <param name="callback">The callback to execute for each item in the array.</param>
        internal static void Array_RemoveAll(FunctionObject ctx, ScriptObject instance, FunctionObject callback, BoxedValue thisArg)
        {
            // Make sure the instance is an array
            var array = instance as ArrayObject;
            if (array == null)
                return;

            lock (array)
            {

                // Get this argument
                var target = thisArg.IsUndefined
                    ? instance
                    : thisArg.Object;

                // Delete and fill the descriptors
                var length = array.Length;
                var current = 0;
                for (uint i = 0; i < length; ++i)
                {
                    // Get the item
                    var item = array.Get(i);

                    // Call the callback and check the result
                    var removeResult = callback.Call(target, item, BoxedValue.Box(i), array);
                    if (removeResult.IsBoolean && removeResult.Bool)
                    {
                        // Invoke the changed event
                        ScriptObject.InvokePropertyChange(array, PropertyChangeType.Delete, current.ToString(), Undefined.Boxed, item);

                        // Delete matched
                        array.QuickDelete(i);

                    }
                    else
                    {
                        // Shift the descriptor
                        array.Dense[current] = array.Dense[i];
                        current++;
                    }

                    if (current < i)
                        array.Dense[i] = default(Descriptor);
                }

                // Swap the dense
                array.Length = (uint)current;
            }
        }

        /// <summary>
        /// Removes all elements in the array.
        /// </summary>
        /// <param name="callback">The callback to execute for each item in the array.</param>
        internal static void Array_Clear(FunctionObject ctx, ScriptObject instance)
        {
            // Make sure the instance is an array
            var array = instance as ArrayObject;
            if (array == null)
                return;

            lock (array)
            {
                // Delete and fill the descriptors
                var length = array.Length;
                for (uint i = 0; i < length; ++i)
                {
                    // Get the item and make sure it's ignored
                    var item = array.Get(i);
                    if (item.IsStrictlyObject)
                        item.Object.Ignore();

                    // Delete matched
                    array.QuickDelete(i);
                }

                // Set the new length
                array.Length = (uint)0;

                // Invoke the changed event
                ScriptObject.InvokePropertyChange(array, PropertyChangeType.Delete, "all", Undefined.Boxed, Undefined.Boxed);
            }
        }

        /// <summary>
        /// The every() method tests whether all elements in the array pass the test implemented by the provided function.
        /// </summary>
        /// <param name="callback">The callback to execute for each item in the array.</param>
        internal static BoxedValue Array_Every(FunctionObject ctx, ScriptObject instance, FunctionObject callback, BoxedValue thisArg)
        {
            // Make sure the instance is an array
            var array = instance as ArrayObject;
            if (array == null)
                return BoxedValue.Box(false);

            // Get this argument
            var target = thisArg.IsUndefined
                ? instance
                : thisArg.Object;

            for (int i = 0; i < array.Length; ++i)
            {
                // Call the callback 
                var passed = callback.Call(target, array.Get(i), BoxedValue.Box(i), array);
                if (!passed.IsBoolean || !passed.Bool)
                {
                    // Failed the test.
                    return BoxedValue.Box(false);
                }
            }

            // Passed the test
            return BoxedValue.Box(true);
        }



        /// <summary>
        /// The swap() method swaps the items at two specified indices and returns whether the swap succeded.
        /// </summary>
        internal static BoxedValue Array_Swap(FunctionObject ctx, ScriptObject instance, BoxedValue index1, BoxedValue index2)
        {
            // Make sure the instance is an array
            var array = instance as ArrayObject;
            uint idx1;
            uint idx2;

            // Validate first
            if (array == null || !index1.IsNumber || !index2.IsNumber)
                return BoxedValue.Box(false);

            // Get the numbers
            idx1 = (uint)index1.Number;
            idx2 = (uint)index2.Number;

            // Make sure the ranges are within the limits
            if (idx1 >= array.Length || idx2 >= array.Length)
                return BoxedValue.Box(false);

            // Get the items
            var item1 = array.Get(idx1);
            var item2 = array.Get(idx2);

            // Set the items
            array.Put(idx2, item1);
            array.Put(idx1, item2);


            // Passed the test
            return BoxedValue.Box(true);
        }

        #endregion

    }
}
