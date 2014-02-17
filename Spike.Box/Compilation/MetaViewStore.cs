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
    public class MetaViewStore : MetaStore<MetaView>
    {
        /// <summary>
        /// Constructs a new repository for a particular application.
        /// </summary>
        /// <param name="app">The application that owns the repository.</param>
        public MetaViewStore(App app) : base(app)
        {
            
        }

        /// <summary>
        /// Creates a new file from a disk file.
        /// </summary>
        /// <param name="key">The key of this file.</param>
        /// <param name="file">The file on the disk.</param>
        /// <returns>The mirror file.</returns>
        public override MetaView CreateInstance(string key, FileInfo file)
        {
            return new MetaView(key, file, this.App);
        }

        /// <summary>
        /// Attempts to get a value by the key.
        /// </summary>
        /// <param name="key">The key of the element to retrieve.</param>
        /// <param name="value">The value placeholder for the element to retrieve.</param>
        /// <returns>Whether the value was found or not.</returns>
        public override bool TryGet(string key, out MetaView value)
        {
            if (!key.EndsWith(MetaExtension.Template))
                key += MetaExtension.Template;
            return base.TryGet(key, out value);
        }
    }


}
