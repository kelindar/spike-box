using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Spike.Box
{
    /// <summary>
    /// Represents a dependancy of a file to another file.
    /// </summary>
    public class MetaDependancy
    {
        private readonly App App;
        private readonly string KeyDependant;
        private readonly string KeyDependancy;

        /// <summary>
        /// Constructs a new instance of a dependancy link.
        /// </summary>
        /// <param name="app">The application that owns the dependancy.</param>
        /// <param name="dependant">The key of the dependant.</param>
        /// <param name="dependancy">The key of the dependancy.</param>
        public MetaDependancy(App app, string dependant, string dependancy)
        {
            this.App = app;
            this.KeyDependancy = dependancy;
            this.KeyDependant = dependant;
        }

        /// <summary>
        /// Gets the parent of the dependancy link.
        /// </summary>
        public IMetaFile Dependant
        {
            get
            {
                // If the key ends with the .ng extension (template)
                if (this.KeyDependant.EndsWith(MetaExtension.Template))
                {
                    // Fetch as a view and return the file
                    MetaView view;
                    if (this.App.Views.TryGet(this.KeyDependant, out view))
                        return view;
                    
                    // Fetch as an element and return the file
                    MetaElement elem;
                    if (this.App.Elements.TryGet(this.KeyDependant, out elem))
                        return elem;


                    return null;
                }

                // If the key ends with the .js extension (script)
                if (this.KeyDependant.EndsWith(MetaExtension.Script))
                {
                    // Fetch as an element and return the file
                    MetaScript script;
                    if (this.App.Scripts.TryGet(this.KeyDependant, out script))
                        return script;
                    
                    return null;
                }

                // Nothing to return
                return null;
            }
        }

        /// <summary>
        /// Gets the target of the dependancy.
        /// </summary>
        public IMetaFile Dependancy
        {
            get
            {
                // If the key ends with the .ng extension (template)
                if (this.KeyDependancy.EndsWith(MetaExtension.Template))
                {
                    // Fetch as an element and return the file
                    MetaElement target;
                    if (this.App.Elements.TryGet(this.KeyDependancy, out target))
                    {
                        // Make sure it's invalidated
                        target.Invalidate();

                        // Return the target
                        return target;
                    }
                    return null;
                }

                // If the key ends with the .js extension (script)
                if (this.KeyDependancy.EndsWith(MetaExtension.Script))
                {
                    // Fetch as an element and return the file
                    MetaScript target;
                    if (this.App.Scripts.TryGet(this.KeyDependancy, out target))
                    {
                        // Make sure it's invalidated
                        target.Invalidate();

                        // Return the target
                        return target;
                    }
                    return null;
                }
                
                // Nothing to return
                return null;
            }
        }

    }
}
