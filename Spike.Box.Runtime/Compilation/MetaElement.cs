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

namespace Spike.Box
{
    /// <summary>
    /// Represents an element directive.
    /// </summary>
    public class MetaElement : MetaFile
    {
        #region Constructors
        private string Code = null;
        private string Text = null;

        /// <summary>
        /// Creates a new element from its original source code.
        /// </summary>
        public MetaElement(string key, FileInfo file, App owner)
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
        /// Gets or sets the script that contains a directive for the element.
        /// </summary>
        public string Script
        {
            get
            {
                // Get the content
                if (this.Content != null)
                    return this.Code;
                return null;
            }
        }


        /// <summary>
        /// Gets or sets the template that contains the view for the element.
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

            // Soft requirement, components don't really need to have a code-behind
            // as it might be purely visual components.
            if(this.CodeBehind != null)
                this.Dependancies.AddCodeBehind(this.CodeBehind);

            // Read the stream and write a string
            using (var stream = new MemoryStream(this.Content))
            using (var reader = new StreamReader(stream))
            using (var body = new StringWriter())
            {
                // Read line by line and render the body which is not dependancy
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!this.Dependancies.TryAddParsed(line))
                        body.WriteLine(line);
                }

                // Create a final text version
                this.Text = body.ToString();

                // Minify HTML
                this.Text = Regex.Replace(this.Text, @"\n|\t", " ");
                this.Text = Regex.Replace(this.Text, @">\s+<", "><").Trim();
            }



            // Create a writer for javascript
            using(var writer = new MetaScriptWriter())
            {
                writer.WriteLine("app.lazy.directive('{0}', ['$parse', function($parse)", this.Name.AsDirectiveName());
                writer.WriteLine("{");
                writer.WriteLine(   "return {");
                writer.WriteLine(       "restrict: 'E',");
                writer.WriteLine(       "transclude: true,");
               

                // If we have a code behind for the element, we need to set up the 
                // controller and a linking function.
                if (this.CodeBehind != null)
                {
                    // Get the type name of the surrogate controller
                    var type = this.CodeBehind.Surrogate.Type;


                    // Set the controller
                    writer.WriteLine("controller: ['$scope', '$server', '$parse', {0}Ctrl],", type);

                    // Linking function that executes the handhshake for each element
                    writer.WriteLine("link: function(scope, element, attrs)");
                    writer.WriteLine("{");


                    //writer.WriteLine("scope.$apply(function(){");
                    foreach (var property in this.CodeBehind.Surrogate.Properties)
                    {
                        // We only handle settable properties
                        if (!property.HasSetter)
                            continue;


                        writer.WriteLine("scope.{0} = $parse(attrs['{0}'])(scope.$parent);", property.Name);                        

                    }
                    //writer.WriteLine("});");

                    //writer.WriteLine("console.debug(scope);");
                    //writer.WriteLine("console.debug(attrs);");
                    //writer.WriteLine("console.log('handshake: " + type + " for parent ' + scope.$parent.$i);");
                    writer.WriteLine("},");


                }

                // Create a completely isolate scope without any bindings
                writer.WriteLine("scope: {},");

                writer.WriteLine(       "templateUrl: 'element/{0}',", this.Key);
                writer.WriteLine(       "replace: true");
                writer.WriteLine(   "};");
                writer.WriteLine("}]);");

                this.Code = writer.ToString();
            }

        }

    }
}
