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
    /// Represents a remote information object.
    /// </summary>
    public class RemoteObject : BaseObject
    {
        #region Constructors
        /// <summary>
        /// Creates a new container that servers as an application container for a scope.
        /// </summary>
        /// <param name="prototype">The prototype to use.</param>
        public RemoteObject(ScriptObject prototype)
            : base(prototype)
        {

        }

        /// <summary>
        /// Creates a new container that servers as an application container for a scope.
        /// </summary>
        /// <param name="prototypeName">The name of the prototype to use.</param>
        public RemoteObject(ScriptContext context)
            : this(context.GetPrototype("Remote"))
        {

        }
        #endregion

        #region Exposed Members
        /// <summary>
        /// Gets the hash code reference of the channel.
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The console object instance.</param>
        /// <param name="eventName">The name of the event.</param>
        internal static BoxedValue GetHashCode(FunctionObject ctx, ScriptObject instance)
        {
            // Return the address
            return BoxedValue.Box(
                Channel.Current.GetHashCode()
                );
        }

        /// <summary>
        /// Gets the ip address of the remote connection.
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The console object instance.</param>
        /// <param name="eventName">The name of the event.</param>
        internal static BoxedValue GetAddress(FunctionObject ctx, ScriptObject instance)
        {
            // Get the first client
            var client = Channel.Current.First;
            if (client == null)
                return Undefined.Boxed;

            // Return the address
            return BoxedValue.Box(
                client.Channel.Address.ToString()
                );
        }

        /// <summary>
        /// Gets the first connection timestamp.
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The console object instance.</param>
        /// <param name="eventName">The name of the event.</param>
        internal static BoxedValue GetConnectedOn(FunctionObject ctx, ScriptObject instance)
        {
            // Get the first client
            var client = Channel.Current.First;
            if (client == null)
                return Undefined.Boxed;

            // Return the address
            return BoxedValue.Box(
                client.Channel.ConnectedOn
                );
        }

        /// <summary>
        /// Gets the session time.
        /// </summary>
        /// <param name="ctx">The function context.</param>
        /// <param name="instance">The console object instance.</param>
        /// <param name="eventName">The name of the event.</param>
        internal static BoxedValue GetConnectedFor(FunctionObject ctx, ScriptObject instance)
        {
            // Get the first client
            var client = Channel.Current.First;
            if (client == null)
                return Undefined.Boxed;

            // Return the address
            return BoxedValue.Box(
                client.Channel.ConnectedFor
                );
        }

        #endregion
    }

}


