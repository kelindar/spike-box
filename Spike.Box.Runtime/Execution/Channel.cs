using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spike.Scripting.Hosting;
using Spike.Scripting.Runtime;
using Spike.Box.Properties;
using Spike.Network;

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
        private readonly ScriptContext Context;

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
            this.Context = context;
        }

        /// <summary>
        /// Gets the date since when the session has become inactive.
        /// </summary>
        public DateTime InactiveSince
        {
            get;
            private set;
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
            var obj = new ClientObject(this.Context.Environment, client);

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
                    Native.Serialize(this.Context.Environment, propertyValue)
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
