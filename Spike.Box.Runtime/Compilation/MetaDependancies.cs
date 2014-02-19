using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Spike.Collections;

namespace Spike.Box
{
    /// <summary>
    /// Represents a dependancy of a file to another file.
    /// </summary>
    public class MetaDependancies : ConcurrentList<MetaDependancy>
    {
        #region Constructor
        private readonly App App;
        private readonly MetaFile Owner;

        /// <summary>
        /// Constructs a new list of dependancies.
        /// </summary>
        /// <param name="app">The owner application.</param>
        /// <param name="owner">The owner file.</param>
        public MetaDependancies(App app, MetaFile owner)
            : base()
        {
            this.App = app;
            this.Owner = owner;
        }
        #endregion

        #region Public Members
        /// <summary>
        /// Adds a new dependancy to the list of dependancies.
        /// </summary>
        /// <param name="dependancy">The dependancy to add.</param>
        public void Add(MetaFile dependancy)
        {
            this.Add(new MetaDependancy(this.App, this.Owner.Key, dependancy.Key));
        }

        /// <summary>
        /// Adds a new dependancy to the list of dependancies.
        /// </summary>
        /// <param name="dependancyKey">The dependancy to add.</param>
        public void Add(string dependancyKey)
        {
            this.Add(new MetaDependancy(this.App, this.Owner.Key, dependancyKey));
        }


        /// <summary>
        /// Adds a new dependancy to the list of dependancies.
        /// </summary>
        /// <param name="script">The dependancy to add.</param>
        public void AddCodeBehind(MetaScript script)
        {
            if (script != null)
            {
                // Add the codebehind as a dependancy
                this.Add(script);
            }
            else
            {
                // Print a warning 
                Service.Logger.Log(LogLevel.Warning, "The associated code-behind file for the view " + this.Owner.Key + " was not found.");
            }
        }

        /// <summary>
        /// Resolves all dependancies and sub-dependancies.
        /// </summary>
        /// <returns>Returns the list of all dependancies for the current file.</returns>
        public IList<MetaDependancy> Resolve()
        {
            var result = new List<MetaDependancy>();
            var stackFrame = new Stack<MetaDependancies>();
            stackFrame.Push(this);

            while (stackFrame.Count > 0)   
            {      
                var current = stackFrame.Pop();
                foreach (var dependancy in current)
                {
                    var target = dependancy.Dependancy;
                    if (target != null)
                    {
                        // Push the target for dependancy analysis
                        stackFrame.Push(target.Dependancies);

                        // Add the dependancy to the result
                        result.Add(dependancy);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Attempts to parse a line, fetching the 'using' and adds to a dependancy.
        /// </summary>
        /// <param name="text">The line of text to parse.</param>
        public bool TryAddParsed(string text)
        {
            try
            {
                // Does it start with using keyword?
                if (text.StartsWith("<!--") && text.EndsWith("-->"))
                {
                    // Do we have the using wrapped in comments?
                    text = text.Substring(4, text.Length - 7).Trim();
                }

                if (!text.StartsWith("using"))
                    return false;

                // The name of the dependancy
                var dependancyName = text.Substring(text.IndexOf(' ')).Trim();

                // Check whether the extension was actually specified in the
                // using directive.
                if (MetaExtension.In(dependancyName))
                {
                    // Was specified, simply add the dependancy with the key
                    this.Add(dependancyName);
                    return true;
                }
                else
                {
                    // Get the possible keys and attempt to infer the extension
                    var ng = dependancyName + MetaExtension.Template;
                    var js = dependancyName + MetaExtension.Script;

                    // Can't be the same as the owner
                    if (this.Owner.Key.StartsWith(dependancyName))
                        return false;

                    // Attempt to infer
                    if (this.App.Elements.Contains(ng))
                        this.Add(new MetaDependancy(this.App, this.Owner.Key, ng));
                    else if (this.App.Scripts.Contains(js))
                        this.Add(new MetaDependancy(this.App, this.Owner.Key, js));
                    else
                        return false;
                    return true;
                }
            }
            catch
            {
                // We didn't manage to parse
                return false;
            }
        }
        #endregion

    }
}
