using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Spike.Box
{
    /// <summary>
    /// Represents an application folder.
    /// </summary>
    internal class AppFolder
    {
        #region Constructor
        // Private stuff
        private readonly App App;
        private readonly string AppPath;
        private readonly int AppPathLen;
        private readonly FileSystemWatcher Watcher;

        // Sub-repositories
        public readonly MetaScriptStore Scripts;
        public readonly MetaViewStore Views;
        public readonly MetaElementStore Elements;

        /// <summary>
        /// Constructs a wrapping directory watcher around a local directory.
        /// </summary>
        /// <param name="application">The application to monitor.</param>
        public AppFolder(App application)
        {
            // Set defaults
            this.App = application;
            this.AppPath = Path.Combine(application.LocalDirectory, "app");
            this.AppPathLen = this.AppPath.Length;
            if (!this.AppPath.EndsWith("\\") && !this.AppPath.EndsWith("/"))
                this.AppPathLen++;

            // Make the repositories
            this.Scripts = new MetaScriptStore(application);
            this.Views = new MetaViewStore(application);
            this.Elements = new MetaElementStore(application);

            // Make a watcher
            this.Watcher = new FileSystemWatcher(this.AppPath);
            this.Watcher.IncludeSubdirectories = true;
            this.Watcher.EnableRaisingEvents = true;
            this.Watcher.Filter = "*.*";
            this.Watcher.Changed += OnChanged;
            this.Watcher.Renamed += OnRenamed;
        }

        /// <summary>
        /// Occurs when a file or directory was renamed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            // Change occured, rescan
            this.Invalidate();
        }

        /// <summary>
        /// Occurs when a file was changed, created or deleted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            // Change occured, rescan
            this.Invalidate();
        }

        #endregion

        /// <summary>
        /// Invalidates the repositories and checks for changed files.
        /// </summary>
        internal void Invalidate()
        {
            // Lock so we don't call invalidate at the same time
            lock (this)
            {
                // Create a file enumeration
                var files = Directory
                    .EnumerateFiles(this.AppPath, "*.*", SearchOption.AllDirectories)
                    .Select(f => new FileInfo(f))
                    .ToDictionary(f => GetKey(f));

                // Enumerate through every file
                foreach (var key in files.Keys)
                {
                    // Get the file and the path without the extension
                    var file = files[key];
                    var path = key.Remove(key.Length - file.Extension.Length);

                    switch (file.Extension)
                    {
                        case ".js": Verify<MetaScript>(key, file, this.Scripts); break;
                        case ".ng":
                            if (this.App.Configuration.HasElement(path))
                            {
                                // It's an element
                                Verify<MetaElement>(key, file, this.Elements);
                            }
                            else
                            {
                                // It's a view
                                Verify<MetaView>(key, file, this.Views);
                            }
                            break;
                    }
                }

                // Purge both
                this.Scripts.Invalidate(files);
                this.Views.Invalidate(files);
                this.Elements.Invalidate(files);
            }
        }

        /// <summary>
        /// Checks whether a file was modified or not.
        /// </summary>
        /// <typeparam name="T">The type of the file.</typeparam>
        /// <param name="key">The key in the repository</param>
        /// <param name="file">The file on the disk.</param>
        /// <param name="repository">The repository to check.</param>
        private void Verify<T>(string key, FileInfo file, MetaStore<T> repository)
            where T : IMetaFile
        {
            // Out value
            T value;

            try
            {
                // Try to get the value from the repository
                if (!repository.TryGet(key, out value))
                {
                    // Create a value and set it to the repository key
                    value = repository.Create(key, file);
                }

            }
            catch { }
        }

        /// <summary>
        /// Gets the key of a file.
        /// </summary>
        /// <param name="file">The file to get the key of.</param>
        /// <returns>The key that can be used for retrieval.</returns>
        private string GetKey(FileInfo file)
        {
            // Substring and invert
            return file.FullName
                .ToLowerInvariant()
                .Substring(this.AppPathLen)
                .Replace('\\', '/');
        }

    }
}
