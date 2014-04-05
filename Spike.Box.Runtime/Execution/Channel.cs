using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spike.Scripting.Hosting;
using Spike.Scripting.Runtime;
using Spike.Box.Properties;
using Spike.Network;
using System.Threading.Tasks;

namespace Spike.Box
{
    /// <summary>
    /// Represents an communication channel that is used to talk to the underlying
    /// remote clients.
    /// </summary>
    public sealed class Channel : DisposableObject
    {
        #region Constructors
        /// <summary>
        /// The scripting context for this channel.
        /// </summary>
        private readonly ScriptContext ScriptContext;

        /// <summary>
        /// The clients within this session.
        /// </summary>
        private readonly ConcurrentDictionary<int, ClientObject> Clients
            = new ConcurrentDictionary<int, ClientObject>();

        /// <summary>
        /// Constructs a new instance of a <see cref="Channel"/>.
        /// </summary>
        /// <param name="context">The execution context that is used to execute scripts.</param>
        internal Channel(ScriptContext context)
        {
            this.ScriptContext = context;
        }

        /// <summary>
        /// Gets the date since when the session has become inactive.
        /// </summary>
        public DateTime InactiveSince
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the context of the channel.
        /// </summary>
        public ScriptContext Context
        {
            get { return this.ScriptContext; }
        }
        #endregion

        #region Public Members
        /// <summary>
        /// Adds a client within this session.
        /// </summary>
        /// <param name="client">The client to add.</param>
        public ClientObject AddClient(IClient client)
        {
            // Get the key
            var cid = client.GetHashCode();
            var obj = new ClientObject(this.ScriptContext.Environment, client);

            // Try add the client
            if (this.Clients.TryAdd(cid, obj))
            {
                client.Disconnect += this.OnDisconnect;
                return obj;
            }
            return null;
        }


        /// <summary>
        /// Removes a client from this session.
        /// </summary>
        /// <param name="client">The client to remove.</param>
        public void RemoveClient(IClient client)
        {
            // Get the key
            var key = client.GetHashCode();

            // Remove the client
            ClientObject removed;
            this.Clients.TryRemove(key, out removed);

            // Unhook
            if(removed.IsAlive)
                removed.Target.Disconnect -= this.OnDisconnect;
        }

        /// <summary>
        /// Send the exception to each console in this session.
        /// </summary>
        /// <param name="ex">The exception to send.</param>
        internal void SendException(Exception ex)
        {
            try
            {
                // Broadcast to each client
                foreach (var clientRef in this.Clients.Values)
                {
                    // Make sure the client is alive and kicking
                    if (!clientRef.IsAlive)
                        continue;

                    // Get the underlying client
                    var client = clientRef.Target;

                    // Forward the send
                    client.SendEventInform((byte)AppEventType.Console, 0, "error",
                        String.Format("{0}: {1}", ex.GetType().Name, ex.Message)
                        );

                }
            }
            catch (Exception e)
            {
                Service.Logger.Log(e);
            }
        }

        /// <summary>
        /// Sends a property change event to all clients within this session.
        /// </summary>
        /// <param name="type">The type of the property to send.</param>
        /// <param name="propertyKey">The property key to send.</param>
        /// <param name="propertyName">The property name to send.</param>
        /// <param name="propertyValue">The property value to send.</param>
        internal void SendPropertyChange(PropertyChangeType type, int propertyKey, string propertyName, BoxedValue propertyValue)
        {
            try
            {
                // Serialize the value to send first
                var serializedValue = TypeConverter.ToNullableString(
                    Native.Serialize(this.ScriptContext.Environment, propertyValue)
                    );

                // Broadcast to each client
                foreach (var clientRef in this.Clients.Values)
                {
                    // Make sure the client is alive and kicking
                    if (!clientRef.IsAlive)
                        continue;

                    // Get the underlying client
                    var client = clientRef.Target;

                    // Depending on the type of the change, send 
                    switch (type)
                    {
                        case PropertyChangeType.Put: client.SendEventInform((byte)AppEventType.PropertyPut, propertyKey, propertyName, serializedValue); break;
                        case PropertyChangeType.Set: client.SendEventInform((byte)AppEventType.PropertySet, propertyKey, propertyName, serializedValue); break;
                        case PropertyChangeType.Delete: client.SendEventInform((byte)AppEventType.PropertyDelete, propertyKey, propertyName, null); break;
                    }
                }
            }
            catch (Exception e)
            {
                // Send the exception.
                this.SendException(e);
            }
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Occurs when a client is disconnected.
        /// </summary>
        void OnDisconnect(IClient client)
        {
            // Make sure we have a client who just disconnected
            if (client == null)
                return;

            // Remove the client from the scope
            this.RemoveClient(client);

            // Check if we should set the inactivity
            if (this.Clients.Count == 0)
                this.InactiveSince = DateTime.UtcNow;
        }

        #endregion

        #region Exposed Members
        /// <summary>
        /// Broadcasts an event to all the clients within this session.
        /// </summary>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="eventValue">The value of the event.</param>
        private void SendEvent(AppEventType eventType, BoxedValue target, string eventName, BoxedValue eventValue)
        {
            try
            {
                // Broadcast to each client
                foreach (var client in this.Clients.Values)
                    Native.SendEvent(client, eventType, target, eventName, eventValue);
            }
            catch (Exception ex)
            {
                // Log the exception
                Service.Logger.Log(ex);
            }
        }
        /// <summary>
        /// Represents low-level networking call that can be used to transmit an event to the browser.
        /// </summary>
        /// <param name="client">The client to transmit the event to.</param>
        /// <param name="propertyName">The name of the event to transmit.</param>
        /// <param name="type">The type of the event to transmit.</param>
        /// <param name="value">The value of the event to transmit.</param>
        internal void DispatchEvent(double type, BoxedValue target, string eventName, BoxedValue value)
        {
            this.SendEvent((AppEventType)type, target, eventName, value);
        }

        /// <summary>
        /// Represents low-level networking call that can be used to transmit a property change notification.
        /// </summary>
        /// <param name="client">The client to transmit the event to.</param>
        /// <param name="eventName">The name of the event to transmit.</param>
        /// <param name="value">The value of the event to transmit.</param>
        internal void DispatchProperty(BoxedValue target, string propertyName, BoxedValue value)
        {
            this.SendEvent(AppEventType.PropertyChange, target, propertyName, value);
        }

        /// <summary>
        /// Represents low-level networking call that can be used to transmit a console notification.
        /// </summary>
        /// <param name="client">The client to transmit the event to.</param>
        /// <param name="eventName">The name of the event to transmit.</param>
        /// <param name="value">The value of the event to transmit.</param>
        internal void DispatchConsole(string methodName, BoxedValue value)
        {
            this.SendEvent(AppEventType.Console, Undefined.Boxed, methodName, value);
        }


        #endregion

        #region Public Members (Callback)
        /// <summary>
        /// Dispatches a callback event, usually onComplete or onError to the single-threaded javascript scheduler.
        /// </summary>
        /// <param name="function">The callback function to dispatch.</param>
        /// <param name="thisInstance">The 'this' parameter value.</param>
        /// <param name="arg1">The argument to pass to the callback.</param>
        public void DispatchCallback(BoxedValue function, ScriptObject thisInstance, BoxedValue arg1)
        {
            Task task;
            if (!function.IsUndefined && function.IsFunction)
                task = this.Context.DispatchAsync(function.Func, thisInstance, new[] { arg1 }, this);
        }

        /// <summary>
        /// Dispatches a callback event, usually onComplete or onError to the single-threaded javascript scheduler.
        /// </summary>
        /// <param name="function">The callback function to dispatch.</param>
        /// <param name="thisInstance">The 'this' parameter value.</param>
        /// <param name="arg1">The argument to pass to the callback.</param>
        /// <param name="arg2">The argument to pass to the callback.</param>
        public void DispatchCallback(BoxedValue function, ScriptObject thisInstance, BoxedValue arg1, BoxedValue arg2)
        {
            Task task;
            if (!function.IsUndefined && function.IsFunction)
                task = this.Context.DispatchAsync(function.Func, thisInstance, new[] { arg1, arg2 }, this);
        }

        /// <summary>
        /// Dispatches a callback event, usually onComplete or onError to the single-threaded javascript scheduler.
        /// </summary>
        /// <param name="function">The callback function to dispatch.</param>
        /// <param name="thisInstance">The 'this' parameter value.</param>
        /// <param name="arg1">The argument to pass to the callback.</param>
        /// <param name="arg2">The argument to pass to the callback.</param>
        /// <param name="arg3">The argument to pass to the callback.</param>
        public void DispatchCallback(BoxedValue function, ScriptObject thisInstance, BoxedValue arg1, BoxedValue arg2, BoxedValue arg3)
        {
            Task task;
            if (!function.IsUndefined && function.IsFunction)
                task = this.Context.DispatchAsync(function.Func, thisInstance, new[] { arg1, arg2, arg3 }, this);
        }

        /// <summary>
        /// Dispatches a callback event, usually onComplete or onError to the single-threaded javascript scheduler.
        /// </summary>
        /// <param name="function">The callback function to dispatch.</param>
        /// <param name="thisInstance">The 'this' parameter value.</param>
        /// <param name="arg1">The argument to pass to the callback.</param>
        /// <param name="arg2">The argument to pass to the callback.</param>
        /// <param name="arg3">The argument to pass to the callback.</param>
        /// <param name="arg4">The argument to pass to the callback.</param>
        public void DispatchCallback(BoxedValue function, ScriptObject thisInstance, BoxedValue arg1, BoxedValue arg2, BoxedValue arg3, BoxedValue arg4)
        {
            Task task;
            if (!function.IsUndefined && function.IsFunction)
                task = this.Context.DispatchAsync(function.Func, thisInstance, new[] { arg1, arg2, arg3, arg4 }, this);
        }

        /// <summary>
        /// Dispatches a callback event, usually onComplete or onError to the single-threaded javascript scheduler.
        /// </summary>
        /// <param name="function">The callback function to dispatch.</param>
        /// <param name="thisInstance">The 'this' parameter value.</param>
        /// <param name="arg1">The argument to pass to the callback.</param>
        /// <param name="arg2">The argument to pass to the callback.</param>
        /// <param name="arg3">The argument to pass to the callback.</param>
        /// <param name="arg4">The argument to pass to the callback.</param>
        /// <param name="arg5">The argument to pass to the callback.</param>
        public void DispatchCallback(BoxedValue function, ScriptObject thisInstance, BoxedValue arg1, BoxedValue arg2, BoxedValue arg3, BoxedValue arg4, BoxedValue arg5)
        {
            Task task;
            if (!function.IsUndefined && function.IsFunction)
                task = this.Context.DispatchAsync(function.Func, thisInstance, new[] { arg1, arg2, arg3, arg4, arg5 }, this);
        }

        /// <summary>
        /// Dispatches a callback event, usually onComplete or onError to the single-threaded javascript scheduler.
        /// </summary>
        /// <param name="function">The callback function to dispatch.</param>
        /// <param name="thisInstance">The 'this' parameter value.</param>
        /// <param name="args">The arguments to pass to the callback.</param>
        public void DispatchCallback(BoxedValue function, ScriptObject thisInstance, BoxedValue[] args)
        {
            Task task;
            if (!function.IsUndefined && function.IsFunction)
                task = this.Context.DispatchAsync(function.Func, thisInstance, args, this);
        }

        /// <summary>
        /// Dispatches a callback event, usually onComplete or onError to the single-threaded javascript scheduler.
        /// </summary>
        /// <param name="function">The callback function to dispatch.</param>
        /// <param name="thisInstance">The 'this' parameter value.</param>
        public void DispatchCallback(BoxedValue function, ScriptObject thisInstance)
        {
            Task task;
            if (!function.IsUndefined && function.IsFunction)
                task = this.Context.DispatchAsync(function.Func, thisInstance, null, this);
        }

        #endregion

        #region Public Members (Threading)

        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="function">The function to execute.</param>
        /// <param name="channel">The channel to execute on.</param>
        /// <returns>The awaitable asynchrounous task.</returns>
        public async Task DispatchAsync(FunctionObject function, ScriptObject thisInstance, BoxedValue arg1)
        {
            // Forward the dispatch
            await this.Context.DispatchAsync(function, thisInstance, new []{ arg1 }, this);
        }

        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="function">The function to execute.</param>
        /// <param name="channel">The channel to execute on.</param>
        /// <returns>The awaitable asynchrounous task.</returns>
        public async Task DispatchAsync(FunctionObject function, ScriptObject thisInstance, BoxedValue arg1, BoxedValue arg2)
        {
            // Forward the dispatch
            await this.Context.DispatchAsync(function, thisInstance, new[] { arg1, arg2 }, this);
        }

        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="function">The function to execute.</param>
        /// <param name="channel">The channel to execute on.</param>
        /// <returns>The awaitable asynchrounous task.</returns>
        public async Task DispatchAsync(FunctionObject function, ScriptObject thisInstance, BoxedValue arg1, BoxedValue arg2, BoxedValue arg3)
        {
            // Forward the dispatch
            await this.Context.DispatchAsync(function, thisInstance, new[] { arg1, arg2, arg3 }, this);
        }

        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="function">The function to execute.</param>
        /// <param name="channel">The channel to execute on.</param>
        /// <returns>The awaitable asynchrounous task.</returns>
        public async Task DispatchAsync(FunctionObject function, ScriptObject thisInstance, BoxedValue arg1, BoxedValue arg2, BoxedValue arg3, BoxedValue arg4)
        {
            // Forward the dispatch
            await this.Context.DispatchAsync(function, thisInstance, new[] { arg1, arg2, arg3, arg4 }, this);
        }

        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="function">The function to execute.</param>
        /// <param name="channel">The channel to execute on.</param>
        /// <returns>The awaitable asynchrounous task.</returns>
        public async Task DispatchAsync(FunctionObject function, ScriptObject thisInstance, BoxedValue arg1, BoxedValue arg2, BoxedValue arg3, BoxedValue arg4, BoxedValue arg5)
        {
            // Forward the dispatch
            await this.Context.DispatchAsync(function, thisInstance, new[] { arg1, arg2, arg3, arg4, arg5 }, this);
        }


        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="function">The function to execute.</param>
        /// <param name="channel">The channel to execute on.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>The awaitable asynchrounous task.</returns>
        public async Task DispatchAsync(FunctionObject function, ScriptObject thisInstance, BoxedValue[] args)
        {
            // Forward the dispatch
            await this.Context.DispatchAsync(function, thisInstance, args, this);
        }

        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="function">The function to execute.</param>
        /// <param name="channel">The channel to execute on.</param>
        /// <returns>The awaitable asynchrounous task.</returns>
        public async Task DispatchAsync(FunctionObject function, ScriptObject thisInstance)
        {
            // Forward the dispatch
            await this.Context.DispatchAsync(function, thisInstance, null, this);
        }

        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="script">The function to execute.</param>
        /// <param name="channel">The channel to execute on.</param>
        /// <returns>The awaitable asynchrounous task.</returns>
        public async Task DispatchAsync(Action script)
        {
            // Forward the dispatch
            await this.Context.DispatchAsync(script, this);
        }

        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="script">The function to execute.</param>
        /// <returns>The awaitable asynchrounous task.</returns>
        public async Task<T> DispatchAsync<T>(Func<T> script)
        {
            // Forward the dispatch
            return await this.Context.DispatchAsync<T>(script, this);
        }


        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="function">The function to execute.</param>
        /// <param name="channel">The channel to execute on.</param>
        /// <returns>The awaitable asynchrounous task.</returns>
        public void Dispatch(FunctionObject function, ScriptObject thisInstance)
        {
            // Forward the dispatch
            this.Context.Dispatch(function, thisInstance, this);
        }

        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="script">The function to execute.</param>
        /// <param name="channel">The channel to execute on.</param>
        /// <returns>The result of the invoke.</returns>
        public void Dispatch(Action script)
        {
            // Forward the dispatch
            this.Context.Dispatch(script, this);
        }
        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="script">The function to execute.</param>
        /// <returns>The result of the invoke.</returns>
        public T Dispatch<T>(Func<T> script)
        {
            // Forward the dispatch
            return this.Context.Dispatch<T>(script, this);
        }


        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="action">The function to execute.</param>
        /// <param name="channel">The channel to execute on.</param>
        /// <returns>The awaitable asynchrounous task.</returns>
        public Task Async(Action action)
        {
            // Forward the dispatch
            return this.Context.StartTask(action, this);
        }

        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="function">The function to execute.</param>
        /// <returns>The awaitable asynchrounous task.</returns>
        public Task<T> Async<T>(Func<T> function)
        {
            // Forward the dispatch
            return this.Context.StartTask<T>(function, this);
        }

        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="action">The function to execute.</param>
        /// <param name="channel">The channel to execute on.</param>
        /// <param name="callback">The callback to execute at the end.</param>
        /// <param name="thisInstance">The 'this' instance to pass to the callback</param>
        /// <returns>The awaitable asynchrounous task.</returns>
        public Task Async(Action action, BoxedValue callback, ScriptObject thisInstance)
        {
            // Forward the dispatch
            return this.Context.StartTask(() =>
            {
                // Execute the action
                action();

                // Dispatch the callback
                this.DispatchCallback(callback, thisInstance);

            }, this);
        }

        /// <summary>
        /// Invokes a function within this context.
        /// </summary>
        /// <typeparam name="T">The expected result of the function.</typeparam>
        /// <param name="function">The function to execute.</param>
        /// <param name="channel">The channel to execute on.</param>
        /// <param name="callback">The callback to execute at the end.</param>
        /// <param name="thisInstance">The 'this' instance to pass to the callback</param>
        /// <returns>The awaitable asynchrounous task.</returns>
        public Task Async(Func<BoxedValue> function, BoxedValue callback, ScriptObject thisInstance)
        {
            // Forward the dispatch
            return this.Context.StartTask(() =>
            {
                // Execute the action and get the result
                var result = function();

                // Dispatch the callback
                this.DispatchCallback(callback, thisInstance, result);

            }, this);
        }


        #endregion

        #region Thread Static Members
        [ThreadStatic]
        private static Channel ThreadChannel;

        /// <summary>
        /// Gets the current session.
        /// </summary>
        public static Channel Current
        {
            get { return ThreadChannel; }
            internal set { ThreadChannel = value; }
        }

        #endregion

        #region IDisposable Members
        /// <summary>
        /// Occurs when the channel object is disposing.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            try
            {
                // Clears the clients
                this.Clients.Clear();
            }
            catch (Exception ex)
            {
                Service.Logger.Log(ex);
            }
        }
        #endregion

    }
}
