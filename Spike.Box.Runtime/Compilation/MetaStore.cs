using System;
using System.Linq;
using Spike.Scripting;
using Spike.Scripting.Runtime;
using Spike.Scripting.Compiler;
using Spike.Scripting.Hosting;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.IO;

namespace Spike.Box
{
    /// <summary>
    /// Represents a repository base class.
    /// </summary>
    public abstract class MetaStore<T> where T : IMetaFile
    {
        #region Constructors
        /// <summary>
        /// The hashtable containing the underlying storage.
        /// </summary>
        protected readonly ConcurrentDictionary<string, T> Store
            = new ConcurrentDictionary<string, T>();

        /// <summary>
        /// The application that owns the repository.
        /// </summary>
        protected readonly App App;

        /// <summary>
        /// Constructs a new repository for a particular application.
        /// </summary>
        /// <param name="app">The application that owns the repository.</param>
        public MetaStore(App app)
        {
            this.App = app;
        }

        #endregion

        #region Public Members
        /// <summary>
        /// Attempts to get a value by the key.
        /// </summary>
        /// <param name="key">The key of the element to retrieve.</param>
        /// <param name="value">The value placeholder for the element to retrieve.</param>
        /// <returns>Whether the value was found or not.</returns>
        public virtual bool TryGet(string key, out T value)
        {
            key = key.ToLowerInvariant();
            return this.Store.TryGetValue(key, out value);
        }

        /// <summary>
        /// Checks whether the specified key is in the repository or not..
        /// </summary>
        /// <param name="key">The key of the element to retrieve.</param>
        /// <returns>Whether the value was found or not.</returns>
        public bool Contains(string key)
        {
            key = key.ToLowerInvariant();
            return this.Store.ContainsKey(key);
        }

        /// <summary>
        /// Sets the value in the store.
        /// </summary>
        /// <param name="key">The key of the element to store.</param>
        /// <param name="value">The value of the element to store.</param>
        public void Set(string key, T value)
        {
            key = key.ToLowerInvariant();
            this.Store.AddOrUpdate(key, value, (k, s) => value);
        }

        /// <summary>
        /// Removes a key from the store.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        public void Remove(string key)
        {
            // Remove the entry
            T dummy;
            this.Store.TryRemove(key, out dummy);
        }

        /// <summary>
        /// Creates and sets the value in the store.
        /// </summary>
        /// <param name="key">The key of the element to store.</param>
        /// <param name="file">The value of the element to store.</param>
        public T Create(string key, FileInfo file)
        {
            var instance = this.CreateInstance(key, file);
            this.Set(key, instance);
            return instance;
        }

        /// <summary>
        /// Removes all the elements that are not in the files hashtable.
        /// </summary>
        /// <param name="files">The hashtable with existing files.</param>
        internal void Invalidate(Dictionary<string, FileInfo> files)
        {
            foreach (var key in this.Store.Keys)
            {
                if (!files.ContainsKey(key))
                {
                    // Remove the entry
                    this.Remove(key);
                }
                else
                {
                    // Invalidate the element
                    T value;
                    if (this.TryGet(key, out value))
                        value.Invalidate();
                }
            }
        }
        #endregion

        #region Abstract Members
        /// <summary>
        /// Creates a new file from a disk file.
        /// </summary>
        /// <param name="key">The key of the file.</param>
        /// <param name="file">The file on the disk.</param>
        /// <returns>The mirror file.</returns>
        public abstract T CreateInstance(string key, FileInfo file);
        #endregion
    }

}
