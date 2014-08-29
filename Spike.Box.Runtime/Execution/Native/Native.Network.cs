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
        #region Internal SendEvent
        /// <summary>
        /// Represents low-level networking call that can be used to transmit an event to the browser.
        /// </summary>
        /// <param name="client">The client to transmit the event to.</param>
        /// <param name="propertyName">The name of the event to transmit.</param>
        /// <param name="type">The type of the event to transmit.</param>
        /// <param name="eventValue">The value of the event to transmit.</param>
        internal static void SendEvent(ClientObject client, AppEventType type, BoxedValue target, string eventName, BoxedValue eventValue)
        {
            try
            {
                // Check if the target is an object and retrieve the id.
                var oid = 0;
                if (target.IsObject && target.Object is BaseObject)
                    oid = ((BaseObject)target.Object).Oid;

                // Get the client
                var channel = client.Target;

                // Convert to a string
                var stringValue = TypeConverter.ToNullableString(
                    Native.Serialize(client.Env, eventValue, true)
                    );

                // Dispatch the inform
                channel.TransmitEvent(type, oid, eventName, stringValue);
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
        /// <param name="eventValue">The value of the event to transmit.</param>
        internal static void SendEvent(BoxedValue client, AppEventType type, BoxedValue target, string eventName, BoxedValue eventValue)
        {
            try
            {
                // Check if it's an object
                if (!client.IsObject)
                    return;

                // Unbox & check if alive
                var unboxedClient = client.Object as ClientObject;
                if (unboxedClient == null && unboxedClient.IsAlive)
                    return;

                // Unboxed call
                Native.SendEvent(unboxedClient, type, target, eventName, eventValue);
            }
            catch (Exception ex)
            {
                // Log the exception
                Service.Logger.Log(ex);
            }
        }


        #endregion

        #region transmitEvent, transmitProperty, transmitConsole
        /// <summary>
        /// Represents low-level networking call that can be used to transmit an event to the browser.
        /// </summary>
        /// <param name="client">The client to transmit the event to.</param>
        /// <param name="propertyName">The name of the event to transmit.</param>
        /// <param name="type">The type of the event to transmit.</param>
        /// <param name="value">The value of the event to transmit.</param>
        internal static void TransmitEvent(BoxedValue client, double type, BoxedValue target, string eventName, BoxedValue value)
        {
            Native.SendEvent(client, (AppEventType)type, target, eventName, value);
        }

        /// <summary>
        /// Represents low-level networking call that can be used to transmit a property change notification.
        /// </summary>
        /// <param name="client">The client to transmit the event to.</param>
        /// <param name="eventName">The name of the event to transmit.</param>
        /// <param name="value">The value of the event to transmit.</param>
        internal static void TransmitProperty(BoxedValue client, BoxedValue target, string propertyName, BoxedValue value)
        {
            Native.SendEvent(client, AppEventType.PropertyChange, target, propertyName, value);
        }

        /// <summary>
        /// Represents low-level networking call that can be used to transmit a console notification.
        /// </summary>
        /// <param name="client">The client to transmit the event to.</param>
        /// <param name="eventName">The name of the event to transmit.</param>
        /// <param name="value">The value of the event to transmit.</param>
        internal static void TransmitConsole(BoxedValue client,  string methodName, BoxedValue value)
        {
            Native.SendEvent(client, AppEventType.Console, Undefined.Boxed, methodName, value);
        }
        #endregion
    }
}
