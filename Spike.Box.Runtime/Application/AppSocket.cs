using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spike.Hubs;
using Spike.Text.Json;
using Spike.Text.Json.Linq;
using Spike.Network;
using Spike.Scripting.Runtime;

namespace Spike.Box
{
    /// <summary>
    /// Represents a communication channel associated with the application.
    /// </summary>
    internal static class AppSocket
    {
        #region Constructor
        /// <summary>
        /// Configures the application socket.
        /// </summary>
        [InvokeAt(InvokeAtType.Configure)]
        public static void Configure()
        {
            // Hook the events
            AppProtocol.Handshake += OnHandshake;
            AppProtocol.Query += OnQuery;
            AppProtocol.Notify += OnNotify;
        }

        #endregion

        /// <summary>
        /// Occurs when a remote client attempts to handshake with a target within the application.
        /// </summary>
        /// <param name="client">The remote client.</param>
        /// <param name="packet">The packet received.</param>
        private static void OnHandshake(IClient client, HandshakeRequest packet)
        {
            try
            {
                // Get the parameters
                var callback = packet.Callback;
                var request = JsonConvert.DeserializeObject<Handshake>(packet.Value);
                if (request == null)
                    return;

                // Attempt to fetch the app
                var app = AppServer.Current.GetByKey(request.Application);
                if (app == null)
                    return;


                // We need to create a view scope, with the provided class type and the
                // view name is actually the prototype, as we only create one scope per
                // session, sharing it accross pages. Subject to change.
                Scope scope = app.Application.Scope
                    .Within<SessionScope>(request.Session)
                    .Within<PageScope>(request.View, request.View);

                // If we are attempting to create an element, we need to create an 
                // element scope, with the provided class type and the name, inside
                // the appropriate view.
                if(request.Type == Handshake.HandshakeType.Element)
                {
                    // Go deeper within scope tree
                    scope = scope.Within<ElementScope>(request.Element, "E" + request.Instance);
                }


                // Reply with the target, only if we have a proper scope
                if(scope != null)
                    client.SendHandshakeInform(callback, scope.Oid);

            }
            catch (Exception ex)
            {
                // Log the exception
                Service.Logger.Log(ex);
            }
        }

        /// <summary>
        /// Occurs when a remote client attempts to query a target within the application.
        /// </summary>
        /// <param name="client">The remote client.</param>
        /// <param name="packet">The packet received.</param>
        private static void OnQuery(IClient client, QueryRequest packet)
        {
            try
            {
                // Get method, arguments and the callback
                var method = packet.Method;
                var arguments = packet.Arguments;
                var callback = packet.Callback;
                var target = packet.Target;

                // Do not query private methods
                if (method.IsPrivateName())
                    return;

                // Retrieves the target from the object lookup cache
                var scope = ScriptMap.Get<PageScope>(target);
                if (scope == null || scope.Channel == null)
                    return;

                try
                {
                    // Make sure we have a thread static session set
                    Channel.Current = scope.Channel;
                    Channel.Current.Caller = client;

                    // Get the session and add a client to it, this 
                    // doesn't do anything if the client already exists
                    // on this session.
                    var session = scope.Channel.AddClient(client);

                    // TODO: Handle arguments
                    var result = scope.CallMethod(method, arguments, Channel.Current);

                    // Return in any case (whether we have a result or not)
                    client.SendQueryInform(callback, result);
                }
                catch (Exception ex)
                {
                    // Send the exception
                    Channel.Current.SendException(ex);
                }
                finally
                {
                    // Reset the scope back to null
                    Channel.Reset();
                }

            }
            catch (Exception ex)
            {
                // Log the exception
                Service.Logger.Log(ex);
            }

        }

        /// <summary>
        /// Occurs when a remote client notifies the server that a property has changed.
        /// </summary>
        /// <param name="client">The remote client.</param>
        /// <param name="packet">The packet received.</param>
        private static void OnNotify(IClient client, NotifyRequest packet)
        {
            try
            {
                // Get the packet parameters
                var target = packet.Target;
                var type   = packet.Type;
                var name   = packet.Name;
                var value  = packet.Value;

                // Retrieves the target from the object lookup cache.
                Channel channel; // Also, attemps to retrieve the scope.
                var instance = ScriptMap.Get(target, out channel);
                if (instance == null)
                    return;

                try
                {
                    // Deserialize the property value
                    var instanceValue = Native.Deserialize(instance.Env, value);

                    // Make sure we have a thread static channel bound
                    Channel.Current = (instance is Scope) 
                        ? (instance as Scope).Channel
                        : channel;

                    // Get the session and add a client to it, this 
                    // doesn't do anything if the client already exists
                    // on this session.
                    if (Channel.Current != null)
                        Channel.Current.AddClient(client);

                    // Set the current caller
                    Channel.Current.Caller = client;
                    Channel.Current.IgnoreForCaller(target, name);

                    // In the case the instance is a scope and we have a setter 
                    // defined, we need to invoke the setter too.
                    if (instance is Scope)
                    {
                        // Get the scope
                        var scope = instance as Scope;

                        // Call the setter
                        scope.CallSetter(name, instanceValue, Channel.Current);
                    }
                    else
                    {
                        // Lock the instance to avoid data races
                        lock (instance)
                        {
                            // Assign to the property without observing the assignment
                            instance.Set(name, instanceValue);
                        }
                    }
                    
                }
                catch (Exception ex)
                {
                    // Send the exception
                    Channel.Current.SendException(ex);
                }
                finally
                {
                    // Reset the scope back to null
                    Channel.Reset();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Service.Logger.Log(ex);
            }
        }

    }

    #region Handshake Type
    /// <summary>
    /// Represents a loosely typed handshake request.
    /// </summary>
    public class Handshake
    {
        /// <summary>
        /// Gets or sets the application identifier.
        /// </summary>
        [JsonProperty("app")]
        public uint Application
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        [JsonProperty("session")]
        public string Session
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the view.
        /// </summary>
        [JsonProperty("view")]
        public string View
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the element.
        /// </summary>
        [JsonProperty("element")]
        public string Element
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the instance id of the element.
        /// </summary>
        [JsonProperty("instance")]
        public long Instance
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the type of the handshake.
        /// </summary>
        public HandshakeType Type
        {
            get
            {
                if (String.IsNullOrWhiteSpace(Element))
                    return HandshakeType.View;
                return HandshakeType.Element;
            }
        }

        /// <summary>
        /// Represents the type of the handshake.
        /// </summary>
        public enum HandshakeType
        {
            /// <summary>
            /// A view attempts to handshake with the server.
            /// </summary>
            View,

            /// <summary>
            /// An element attempts to handshake with the server.
            /// </summary>
            Element
        }


    }
        #endregion
}
