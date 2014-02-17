using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Spike.Box
{
    /// <summary>
    /// Represents an application-related file.
    /// </summary>
    public abstract class MetaFile : IMetaFile
    {
        private string   CachedKey;
        private byte[]   CachedContent = null;
        private DateTime CachedTime = DateTime.MinValue;
        private readonly FileInfo Info;
        private readonly MetaDependancies DependsOn;
        /// <summary>
        /// Gets the owner app.
        /// </summary>
        protected readonly App App;

        /// <summary>
        /// Constructs a new file wrapper around a file info.
        /// </summary>
        /// <param name="key">The key of this file, with the namespace.</param>
        /// <param name="file">The file info to wrap.</param>
        /// <param name="owner">The owner app for this file.</param>
        public MetaFile(string key, FileInfo file, App owner)
        {
            this.Info = file;
            this.App = owner;
            this.CachedKey = key;
            this.DependsOn = new MetaDependancies(owner, this);
        }

        /// <summary>
        /// Gets an instance of the parent directory.
        /// </summary>
        public DirectoryInfo Directory 
        {
            get { return this.Info.Directory; }
        }

        /// <summary>
        /// Gets a value indicating whether a file exists.
        /// </summary>
        public bool Exists
        {
            get { return this.Info.Exists; }
        }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        public string Name
        {
            get { return this.Info.Name; }
        }

        /// <summary>
        /// Gets the key of the file.
        /// </summary>
        public string Key
        {
            get { return this.CachedKey; }
        }

        /// <summary>
        /// Gets the string representing the extension part of the file.
        /// </summary>
        public string Extension
        {
            get { return this.Info.Extension; }
        }

        /// <summary>
        /// Gets the full path of the file.
        /// </summary>
        public string FullName
        {
            get { return this.Info.FullName; }
        }

        /// <summary>
        /// Gets the content of the file, or null if no content was found.
        /// </summary>
        public byte[] Content
        {
            get
            {
                // If there's no such file
                if (!this.Exists)
                    return null;

                // Return the cached
                return this.CachedContent;
            }
        }

        /// <summary>
        /// Gets the dependancies of the current file.
        /// </summary>
        public MetaDependancies Dependancies
        {
            get { return this.DependsOn; }
        }

        /// <summary>
        /// Occurs when the content was just reloaded. This occurs lazily
        /// when <see cref="Content"/> property is touched.
        /// </summary>
        protected  virtual void OnContentChange()
        {

        }

        /// <summary>
        /// Refreshes the content if necessary, invoking the appropriate handlers.
        /// </summary>
        public void Invalidate()
        {
            try
            {
                // Gets the content
                if (File.GetLastWriteTimeUtc(this.Info.FullName) > this.CachedTime)
                {
                    // Content changed
                    this.CachedContent = File.ReadAllBytes(this.FullName);
                    this.CachedTime = File.GetLastWriteTimeUtc(this.FullName);

                    // Notify
                    this.OnContentChange();
                }
            }
            catch { }

        }

    }

    /// <summary>
    /// Represents a contract for an application file.
    /// </summary>
    public interface IMetaFile
    {
        /// <summary>
        /// Gets an instance of the parent directory.
        /// </summary>
        DirectoryInfo Directory { get; }

        /// <summary>
        /// Gets a value indicating whether a file exists.
        /// </summary>
        bool Exists { get; }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the string representing the extension part of the file.
        /// </summary>
        string Extension { get; }

        /// <summary>
        /// Gets the full path of the file.
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Gets the content of the file, or null if no content was found.
        /// </summary>
        byte[] Content { get; }
        
        /// <summary>
        /// Gets the dependancies of the current file.
        /// </summary>
        MetaDependancies Dependancies { get; }

        /// <summary>
        /// Refreshes the content if necessary, invoking the appropriate handlers.
        /// </summary>
        void Invalidate();
    }


}
