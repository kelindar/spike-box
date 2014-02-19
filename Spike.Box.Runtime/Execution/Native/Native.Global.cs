using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spike.Network;
using Spike.Scripting.Native;
using Spike.Scripting.Runtime;
using Env = Spike.Scripting.Runtime.Environment;

namespace Spike.Box
{
    /// <summary>
    /// Represents a class that contains native interop.
    /// </summary>
    public partial class Native
    {
        #region setTimeout, setInterval, clearTimeout

        /// <summary>
        /// Calls a function or executes a code snippet repeatedly, with a fixed time delay between each call to that function..
        /// </summary>
        /// <param name="function">The function to call.</param>
        /// <param name="timeout">The number of milliseconds (thousandths of a second) that the setInterval() function should wait before each call to func. As with setTimeout, there is a minimum delay enforced.</param>
        /// <returns>A unique interval ID you can pass to clearInterval()..</returns>
        internal static ScriptObject SetInterval(FunctionObject function, double timeout, BoxedValue param1, BoxedValue param2, BoxedValue param3)
        {
            var channel = Channel.Current;
            var timer = Timer.PeriodicCall(TimeSpan.FromMilliseconds(timeout), () =>
            {
                try
                {
                    // Make sure we have a thread static session set
                    Channel.Current = channel;

                    // This is global object in this case.
                    function.Call(function.Env.Globals, param1, param2, param3);
                }
                catch (Exception ex)
                {
                    // Send the exception
                    Channel.Current.SendException(ex);
                }
                finally
                {
                    // Reset the scope back to null
                    Channel.Current = null;
                }
            });

            // Return the timer as a reference
            return timer.AsReference(function.Env);
        }

        /// <summary>
        /// Calls a function or executes a code snippet after a specified delay.
        /// </summary>
        /// <param name="function">The function to call.</param>
        /// <param name="timeout"></param>
        /// <returns>The timer identifier.</returns>
        internal static ScriptObject SetTimeout(FunctionObject function, double timeout, BoxedValue param1, BoxedValue param2, BoxedValue param3)
        {
            var channel = Channel.Current;
            var timer = Timer.DelayCall(TimeSpan.FromMilliseconds(timeout), () =>
            {
                try
                {
                    // Make sure we have a thread static session set
                    Channel.Current = channel;

                    // This is global object in this case.
                    function.Call(function.Env.Globals, param1, param2, param3);
                }
                catch (Exception ex)
                {
                    // Send the exception
                    Channel.Current.SendException(ex);
                }
                finally
                {
                    // Reset the scope back to null
                    Channel.Current = null;
                }
            });

            // Return the timer as a reference
            return timer.AsReference(function.Env);
        }

        /// <summary>
        /// Clears the delay set by setTimeout()
        /// </summary>
        /// <param name="timerID">The timer identifier to cancel.</param>
        internal static void ClearTimeout(BoxedValue timerID)
        {
            if (!timerID.IsObject)
                return;

            var reference = timerID.Object as ReferenceObject<Timer>;
            if (!reference.IsAlive)
                return;

            var timer = reference.Target as Timer;
            if (timer == null)
                return;

            timer.Stop();
        }

        #endregion

        #region btoa, atob
        /// <summary>
        /// Creates a base-64 encoded ASCII string from a "string" of binary data.
        /// </summary>
        /// <param name="value">The string to process.</param>
        /// <returns>Encoded data.</returns>
        internal static BoxedValue BtoA(BoxedValue value)
        {
            if (!value.IsString)
                return Undefined.Boxed;

            return BoxedValue.Box(
                Convert.ToBase64String(
                Encoding.UTF8.GetBytes(value.String)
                ));
        }

        /// <summary>
        /// Clears the delay set by setTimeout()
        /// </summary>
        /// <param name="value">The string to process.</param>
        /// <returns>Decoded data.</returns>
        internal static BoxedValue AtoB(BoxedValue value)
        {
            if (!value.IsString)
                return Undefined.Boxed;

            return BoxedValue.Box(
                Encoding.UTF8.GetString(
                Convert.FromBase64String(value.String)
                ));
        }
        #endregion

        #region session
        /// <summary>
        /// Gets the current channel.
        /// </summary>
        internal static BoxedValue GetChannel()
        {
            return BoxedValue.Box(Channel.Current);
        }

        #endregion

        #region serialize, deserialize, invoke

        /// <summary>
        /// Invokes a method on this scope, with the provided arguments.
        /// </summary>
        /// <param name="scope">The scope to invoke the method on.</param>
        /// <param name="methodName">The method to invoke.</param>
        /// <param name="args">The arguments to pass to this method.</param>
        /// <returns>The result as a string or a JSON string.</returns>
        public static string Invoke(Scope scope, string methodName, IList<string> args)
        {
            // Prepare the arguments
            BoxedValue[] arguments = null;
            if (args != null && args.Count > 0)
            {
                arguments = new BoxedValue[args.Count];
                for (int i = 0; i < args.Count; ++i)
                    arguments[i] = Native.Deserialize(scope.Env, args[i]);
            }

            // Get the method to
            var method = scope.GetT<FunctionObject>(methodName);
            if (method != null)
            {
                // Call the method and get the result
                var result = arguments == null 
                    ? method.Call(scope)
                    : method.Call(scope, arguments);

                // Serialize the result in JSON format and return
                return TypeConverter.ToNullableString(
                    Native.Serialize(scope.Env, result)
                    );
            }

            // Not found
            return null;
        }


        /// <summary>
        /// Serializes a value in the JSON format.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>Serialized value.</returns>
        internal static BoxedValue Serialize(Env environment, BoxedValue value)
        {
            // If undefined or null, return a boxed null value
            if (value.IsUndefined || value.IsNull)
                return Env.BoxedNull;

            try
            {
                // Get the stringify method
                var stringify = environment.Globals
                    .GetT<ScriptObject>("JSON")
                    .GetT<FunctionObject>("stringify");

                // Call the serialize
                return stringify.Call(null, value);
            }
            catch
            {
                // Return null
                return Env.BoxedNull;
            }
        }

        /// <summary>
        /// Deserializes value from JSON format.
        /// </summary>
        /// <param name="value">The value to deserialize.</param>
        /// <returns>Deserialized value.</returns>
        internal static BoxedValue Deserialize(Env environment, string value)
        {
            try
            {
                // Get the parse method
                var parse = environment.Globals
                    .GetT<ScriptObject>("JSON")
                    .GetT<FunctionObject>("parse");

                // Call parse
                return parse.Call(null, value);
            }
            catch
            {
                // Return null
                return Env.BoxedNull;
            }
        }

        /// <summary>
        /// Invalidates a computed property by executing its getter and sends the value to the
        /// client.
        /// </summary>
        /// <param name="property"></param>
        /*internal static void Invalidate(BoxedValue property)
        {
            // Property can only be a function
            if (!property.IsFunction)
            {
                SessionScope.Current.SendException(
                    new InvalidCastException("invalidate() can only be passed a getter function to invalidate as a parameter.")
                    );
                return;
            }

            // Call the getter
            var result = property.Func.Call();
            if(

        }*/
        #endregion
    }
}
