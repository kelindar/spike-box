using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spike.Scripting.Runtime;

namespace Spike.Box
{
    /// <summary>
    /// Represents an script map that can be used for lookup and metadata storage.
    /// </summary>
    internal sealed class ScriptMap
    {
        #region Constructor
        /// <summary>
        /// Gets the cache for a fast lookup by object id.
        /// </summary>
        private static readonly ConcurrentDictionary<int, ScriptMapEntry> Registry =
            new ConcurrentDictionary<int, ScriptMapEntry>();

        /// <summary>
        /// Static constructor, hooks the events.
        /// </summary>
        static ScriptMap()
        {
            ScriptObject.Observed += Map;
            ScriptObject.Ignored += Unmap;
        }

        #endregion

        #region Public Members (Get)
        /// <summary>
        /// Gets an object by the object identifier.
        /// </summary>
        /// <typeparam name="T">The type of the object to get.</typeparam>
        /// <param name="oid">The object identifier.</param>
        /// <returns>The instance of a retrieved object, or null.</returns>
        public static ScriptObject Get(int oid)
        {
            return Get<ScriptObject>(oid);
        }

        /// <summary>
        /// Gets an object by the object identifier.
        /// </summary>
        /// <typeparam name="T">The type of the object to get.</typeparam>
        /// <param name="oid">The object identifier.</param>
        /// <returns>The instance of a retrieved object, or null.</returns>
        public static T Get<T>(int oid)
            where T : ScriptObject
        {
            Channel channel;
            return Get<T>(oid, out channel);
        }

        /// <summary>
        /// Gets an object by the object identifier.
        /// </summary>
        /// <typeparam name="T">The type of the object to get.</typeparam>
        /// <param name="oid">The object identifier.</param>
        /// <returns>The instance of a retrieved object, or null.</returns>
        public static ScriptObject Get(int oid, out Channel channel)
        {
            return Get<ScriptObject>(oid, out channel);
        }

        /// <summary>
        /// Gets an object by the object identifier.
        /// </summary>
        /// <typeparam name="T">The type of the object to get.</typeparam>
        /// <param name="oid">The object identifier.</param>
        /// <returns>The instance of a retrieved object, or null.</returns>
        public static T Get<T>(int oid, out Channel channel)
            where T : ScriptObject
        {
            // Scope as null
            channel = null;

            // TODO: Purge to remove garbage, from time to time
            ScriptMapEntry entry;
            if (Registry.TryGetValue(oid, out entry))
            {
                // Get the value
                if (entry.IsAlive)
                {
                    channel = entry.Channel;
                    return entry.Value as T;
                }

                // Remove the value
                ScriptMap.Unmap(oid);
                return null;
            }
            return null;
        }
        #endregion

        #region Public Members (Add/Remove)
        /// <summary>
        /// Adds the object to the lookup cache.
        /// </summary>
        /// <param name="instance"></param>
        public static void Map(ScriptObject instance)
        {
            // Add the object to the cache
            ScriptMap.Registry.TryAdd(instance.Oid, new ScriptMapEntry(Channel.Current, instance));
        }

        /// <summary>
        /// Removes the object from the lookup cache.
        /// </summary>
        /// <param name="instance"></param>
        public static void Unmap(ScriptObject instance)
        {
            ScriptMap.Unmap(instance.Oid);
        }

        /// <summary>
        /// Removes the object from the lookup cache.
        /// </summary>
        /// <param name="oid">The key of the object to unmap.</param>
        public static void Unmap(int oid)
        {
            // Make sure we have removed the value from quick access cache
            ScriptMapEntry entry;
            ScriptMap.Registry.TryRemove(oid, out entry);
        }
        #endregion
    }

    #region Entry
    /// <summary>
    /// Represents an entry in the script map.
    /// </summary>
    internal sealed class ScriptMapEntry
    {
        /// <summary>
        /// Creates a new entry with the scope and the reference.
        /// </summary>
        /// <param name="channel">The channel associated with the value.</param>
        /// <param name="value">The reference value.</param>
        public ScriptMapEntry(Channel scope, ScriptObject value)
        {
            this.RefChannel = new WeakReference<Channel>(scope);
            this.RefValue = new WeakReference<ScriptObject>(value);
        }

        /// <summary>
        /// Gets the reference to the scope.
        /// </summary>
        private readonly WeakReference<Channel> RefChannel;

        /// <summary>
        /// Gets the reference to the value.
        /// </summary>
        private readonly WeakReference<ScriptObject> RefValue;

        /// <summary>
        /// Gets whether the entry reference is alive.
        /// </summary>
        public bool IsAlive
        {
            get { return this.RefValue.IsAlive; }
        }

        /// <summary>
        /// Gets the value of the associated object, or null if none was found.
        /// </summary>
        public ScriptObject Value
        {
            get
            {
                if (!this.IsAlive)
                    return null;
                return this.RefValue.Target;
            }
        }

        /// <summary>
        /// Gets the channel of the associated object, or null if none was found.
        /// </summary>
        public Channel Channel
        {
            get
            {
                if (!this.IsAlive)
                    return null;
                return this.RefChannel.Target;
            }
        }
    }
    #endregion
}
