using System;
using System.Linq;
using Spike.Scripting;
using Spike.Scripting.Runtime;
using Spike.Scripting.Compiler;
using Spike.Scripting.Hosting;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Spike.Box.Properties;

namespace Spike.Box
{
    public class MetaScript : MetaFile
    {
        #region Constructors
        private string SourceServer;
        private string SourceClient;
        private string SourceElement;
        private Surrogate Proxy;

        /// <summary>
        /// Creates a new script from an original source.
        /// </summary>
        /// <param name="file">The file info to wrap.</param>
        /// <param name="owner">The owner app for this file.</param>
        public MetaScript(string key, FileInfo file, App owner) : base(key, file, owner)
        {

        }
        #endregion

        /// <summary>
        /// Gets or sets the source of the script to be executed on the server.
        /// </summary>
        public string CodeBehind
        {
            get 
            {
                // Make sure we call the content and refresh the underlying source
                if (this.Content == null)
                    return null;

                return this.SourceServer; 
            }
        }

        /// <summary>
        /// Gets or sets the source of the script.
        /// </summary>
        public string View
        {
            get 
            {
                // Make sure we call the content and refresh the underlying source
                if (this.Content == null)
                    return null;

                return this.SourceClient; 
            }
        }

        /// <summary>
        /// Gets or sets the source of the script
        /// </summary>
        public string Element
        {
            get
            {
                // Make sure we call the content and refresh the underlying source
                if (this.Content == null)
                    return null;

                return this.SourceElement;
            }
        }


        /// <summary>
        /// Gets or sets the surrogate for the script.
        /// </summary>
        internal Surrogate Surrogate
        {
            get
            {
                // Make sure we call the content and refresh the underlying source
                if (this.Content == null)
                    return null;

                return this.Proxy;
            }
        }



        /// <summary>
        /// Occurs when the content was just reloaded. This occurs lazily
        /// when <see cref="Content"/> property is touched.
        /// </summary>
        protected override void OnContentChange()
        {
            // Call the base
            base.OnContentChange();

            // Update using the content
            this.Update(
                Encoding.UTF8.GetString(this.Content)
                );
        }

        /// <summary>
        /// Updates the script with the new source.
        /// </summary>
        /// <param name="serverScript">The server-side source to use.</param>
        private void Update(string serverScript)
        {
            // Set the source to update
            this.SourceServer = serverScript;

            // Include the script and get the surrogate (proxy) to compile
            this.Proxy = this.App.Scope.Include(this);

            // Compile surrogate in different ways and cache it
            if (this.Proxy != null)
            {
                // Compile the source for the client view
                this.SourceClient = Surrogate.Compile(SurrogateType.ViewSurrogate, this.Proxy);

                // Compile the source for the element (directive)
                this.SourceElement = Surrogate.Compile(SurrogateType.ElementSurrogate, this.Proxy);
            }
        }


    }


}
