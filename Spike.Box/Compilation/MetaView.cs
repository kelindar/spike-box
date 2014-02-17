using System;
using System.Linq;
using Spike.Scripting;
using Spike.Scripting.Runtime;
using Spike.Scripting.Compiler;
using Spike.Scripting.Hosting;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using Spike.Text;

namespace Spike.Box
{
    /// <summary>
    /// Represents a view template.
    /// </summary>
    public class MetaView : MetaFile
    {
        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        private string Text = null;

        /// <summary>
        /// Creates a new view from an original source.
        /// </summary>
        public MetaView(string key, FileInfo file, App owner)
            : base(key, file, owner)
        {

        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the code behind script, if found.
        /// </summary>
        public MetaScript CodeBehind
        {
            get 
            {
                // The key is the same, but with different extension
                MetaScript script;
                var scriptKey = this.Key.Replace(MetaExtension.Template, MetaExtension.Script);

                // Try to fetch it from the scripts
                if (this.App.Scripts.TryGet(scriptKey, out script))
                    return script;
                return null;
            }
        }

        /// <summary>
        /// Gets or sets the source template of the view.
        /// </summary>
        public string Template
        {
            get
            {
                // Get the content
                if (this.Content != null)
                    return this.Text;
                return null;
            }
        }
        #endregion

        /// <summary>
        /// Occurs when the content was just reloaded. This occurs lazily
        /// when <see cref="Content"/> property is touched.
        /// </summary>
        protected override void OnContentChange()
        {
            // Call the base
            base.OnContentChange();

            // Each view is dependant on the code-behind script, with the same name
            this.Dependancies.Clear();
            this.Dependancies.AddCodeBehind(this.CodeBehind);

            // Read the stream and write a string
            using (var stream = new MemoryStream(this.Content))
            using (var reader = new StreamReader(stream))
            using (var code = new StringWriter())
            using (var body = new StringWriter())
            {
                // Read line by line and render the body which is not dependancy
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if(!this.Dependancies.TryAddParsed(line))
                        body.WriteLine(line);
                }

                // Resolve all dependancies for the view
                var dependancies = this.Dependancies.Resolve();

                // Render all dependancies
                foreach (var link in dependancies)
                {
                    // Get the dependant and the dependancy
                    var dependant  = link.Dependant;
                    var dependancy = link.Dependancy;

                    // We should render an element (component) which actually
                    // generates javascript angularjs directive.
                    if (dependancy is MetaElement)
                        code.WriteLine((dependancy as MetaElement).Script);

                    // We should render a script, being a code-behind script
                    // or any other script that should be included
                    if (dependancy is MetaScript)
                    {
                        // If the dependant is a view, we should write the
                        // appropriate view script.
                        if(dependant is MetaView)
                            code.WriteLine((dependancy as MetaScript).View);

                        // If the dependant is a view, we should write the
                        // appropriate element script.
                        if (dependant is MetaElement)
                            code.WriteLine((dependancy as MetaScript).Element);
                    }
                }

                // Minify the embedded javascript
                var minify = new Minifier();
                var script = "<script type='application/javascript'>" + 
                    minify.MinifyJavaScript(code.ToString()) +
                    "</script>";

                // Minify the HTML template
                var htmlBody = body.ToString();
                htmlBody = Regex.Replace(htmlBody, @"\n|\t", " ");
                htmlBody = Regex.Replace(htmlBody, @">\s+<", "><").Trim();

                // Create a final text version
                this.Text = script + htmlBody;
            }

        }

   

    }
}
