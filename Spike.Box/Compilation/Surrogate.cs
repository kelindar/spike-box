using System;
using System.Linq;
using Spike.Scripting;
using Spike.Scripting.Runtime;
using Spike.Scripting.Compiler;
using Spike.Scripting.Hosting;
using System.Collections.Generic;
using System.Text;

namespace Spike.Box
{
    /// <summary>
    /// Represents a proxy logic.
    /// </summary>
    internal class Surrogate
    {
        /// <summary>
        /// Constructs a new instance of a proxy script.
        /// </summary>
        /// <param name="name">The name of the proxy script.</param>
        /// <param name="type">The type of the proxy script (javascript class name).</param>
        /// <param name="members">The proxy functions inside.</param>
        public Surrogate(string name, string type, SurrogateMember[] members)
        {
            this.Name = name;
            this.Type = type;
            this.Members = members;

            this.Properties = members.ToArray<SurrogateMember, SurrogateProperty>();
            this.Methods = members.ToArray<SurrogateMember, SurrogateFunction>();
        }

        /// <summary>
        /// Gets the name of the proxy.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Gets the name of the proxy class.
        /// </summary>
        public readonly string Type;

        /// <summary>
        /// Gets the members of the proxy class.
        /// </summary>
        public readonly SurrogateMember[] Members;

        /// <summary>
        /// Gets the properties of the proxy class.
        /// </summary>
        public readonly SurrogateProperty[] Properties;

        /// <summary>
        /// Gets the functions of the proxy class.
        /// </summary>
        public readonly SurrogateFunction[] Methods;

        #region Code Generator

        /// <summary>
        /// Compiles several proxies and returns a javascript string.
        /// </summary>
        /// <param name="proxies">The proxies to compile in a single file.</param>
        /// <param name="type">The type of the surrogate to compile.</param>
        /// <returns>The compiled output.</returns>
        public static string Compile(SurrogateType type, Surrogate proxy)
        {
            using (var writer = new MetaScriptWriter())
            {

                //writer.WriteLine("app.controllerProvider.register('{0}', function ($scope, $server)", proxy.Type);
                writer.WriteLine("function {0}Ctrl ($scope, $server, $parse)", proxy.Type);
                writer.WriteLine("{");

                // For each property:
                foreach (var prop in proxy.Properties.Where(p => p.Modifier == SurrogateMemberModifier.Public))
                {
                    // A function that attaches a property value to the scope
                    writer.WriteLine("var attach_{0} = function(value)", prop.Name, proxy.Type, proxy.Name);
                    writer.WriteLine("{");
                    writer.WriteLine(   "$scope.{0} = value;", prop.Name);
                    writer.WriteLine("};");


                    // If we have a getter, add the querying part
                    if (prop.HasGetter)
                    {
                        // Make the getter
                        writer.WriteLine("$scope.get{0} = function()", prop.Name.UppercaseFirst());
                        writer.WriteLine("{");
                        writer.WriteLine(   "$server.query($scope.$i, '{0}', null, attach_{0});", prop.Name);
                        writer.WriteLine("};");
                    }

                    // If we have a setter, allow the setter to notify the server
                    if (prop.HasSetter)
                    {

                        // Make the getter and setter
                        writer.WriteLine("$scope.set{0} = function()", prop.Name.UppercaseFirst());
                        writer.WriteLine("{");
                        //writer.WriteLine("$server.onPropertySet($scope.$i, '{0}', null, attach_{0});", prop.Name);
                        writer.WriteLine("};");
                    }


                    // Insert an empty property
                    writer.WriteLine("$scope.{0} = null;", prop.Name);
                    writer.WriteLine();

                }

                // For each function:
                foreach (var func in proxy.Methods.Where(p => p.Modifier == SurrogateMemberModifier.Public))
                {
                    writer.Write("var set_{0} = function(o)", func.Name, proxy.Type, proxy.Name);
                    writer.WriteLine("{");
                    writer.WriteLine("$scope.result.{0} = o;", func.Name);
                    writer.WriteLine("};");

                    // Make the function
                    writer.Write("$scope.{0} = function()", func.Name);
                    writer.WriteLine("{");
                    writer.WriteLine("$server.query($scope.$i, '{0}', $server.makeArgs(arguments), set_{0});", func.Name);
                    writer.WriteLine("};");
                }


                // Gets the handhsake and populates the oid scope
                writer.WriteLine("$scope.$$w = angular.watchObject;");

                writer.WriteLine("var onConstruct = function(oid){");
                writer.WriteLine("$scope.$i = oid;");
                writer.WriteLine("$scope.result = new Object();");
                foreach (var prop in proxy.Properties.Where(p => p.Modifier == SurrogateMemberModifier.Public))
                {
                    // For a code-behind of a view only:
                    if (type == SurrogateType.ViewSurrogate)
                    {
                        // Call the getter once
                        if (prop.HasGetter)
                            writer.WriteLine("$scope.get{0}();", prop.Name.UppercaseFirst());
                    }

                    // If there's a public setter, attach angular.watchObject
                    if (prop.HasSetter)
                    {
                        // In any case, we need to be able to watch the property change and propagate it to 
                        // the server.
                        writer.WriteLine("$scope.$$w($server, $parse, '{0}', $server.onPropertySet);", prop.Name);

                        // The element should propagate all properties set originally via its 
                        // attributes back to the server, as they might have been bound to some
                        // UI elements and values.
                        if (type == SurrogateType.ElementSurrogate)
                        {
                            // set the new value arguments
                            writer.WriteLine("var propertyValue = new Object();");
                            writer.WriteLine("propertyValue.target = oid;");
                            writer.WriteLine("propertyValue.name = '{0}';", prop.Name);
                            writer.WriteLine("propertyValue.value = $scope['{0}'];", prop.Name);

                            // Notify the server
                            writer.WriteLine("$server.onPropertySet(propertyValue);");

                        }
                    }
                }
                writer.WriteLine("};");

                // Add a property to the scope with the name
                writer.WriteLine("$scope.$type = '{0}';", proxy.Type);

                // Call the view constructor
                if (type == SurrogateType.ViewSurrogate)
                    writer.WriteLine("$server.view('{0}', onConstruct);", proxy.Type);

                // Call the element constructor
                if (type == SurrogateType.ElementSurrogate)
                    writer.WriteLine("$server.element('{0}', $scope.$parent.$type, onConstruct);", proxy.Type);

                // Handles the property change event
                writer.WriteLine("$scope.$on('e:property', function (e, args) {");
                writer.WriteLine("if(typeof($scope.$i) === 'undefined'){return;}");
                writer.WriteLine("if($scope.$i == args.target && $scope.hasOwnProperty(args.name))");
                writer.WriteLine(   "$scope[args.name] = $server.deserialize(args.value);");
                writer.WriteLine("});");


                writer.WriteLine("};");
                writer.WriteLine();

                writer.WriteLine("{0}Ctrl.$inject = ['$scope', '$server', '$parse'];", proxy.Type);

                // Register as a controller
                writer.WriteLine("app.lazy.controller('{0}', {0}Ctrl);", proxy.Type);
                
                return writer.ToString();
            }
        }

        #endregion
    }


}
