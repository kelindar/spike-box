﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34011
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Spike.Box.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Spike.Box.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!DOCTYPE html&gt;
        ///&lt;html lang=&quot;en&quot; xmlns=&quot;http://www.w3.org/1999/xhtml&quot; ng-app=&quot;app&quot; ng-controller=&quot;box&quot; ng-init=&quot;bind({{app}}, {{view}})&quot;&gt;
        ///&lt;head&gt;
        ///    &lt;meta charset=&quot;utf-8&quot; /&gt;
        ///    &lt;title&gt;{{title}}&lt;/title&gt;
        ///
        ///    &lt;script src=&quot;/js/jquery.min.js&quot; type=&quot;application/javascript&quot;&gt;&lt;/script&gt;
        ///    &lt;script src=&quot;/js/angular.js&quot; type=&quot;application/javascript&quot;&gt;&lt;/script&gt;
        ///	&lt;script src=&quot;/js/angular-route.js&quot; type=&quot;application/javascript&quot;&gt;&lt;/script&gt;
        ///	&lt;script src=&quot;/js/angular-animate.js&quot; type=&quot;application/javascript&quot;&gt;&lt;/scrip [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string app {
            get {
                return ResourceManager.GetString("app", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to // Create a JSON object only if one does not already exist. We create the
        ///// methods in a closure to avoid creating global variables.
        ///
        ///if (typeof JSON !== &apos;object&apos;) {
        ///    JSON = {};
        ///}
        ///
        ///(function () {
        ///    &apos;use strict&apos;;
        ///
        ///    function f(n) {
        ///        // Format integers to have at least two digits.
        ///        return n &lt; 10 ? &apos;0&apos; + n : n;
        ///    }
        ///
        ///    if (typeof Date.prototype.toJSON !== &apos;function&apos;) {
        ///
        ///        Date.prototype.toJSON = function () {
        ///
        ///            return isFinite(this.valueOf())
        ///       [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string Json {
            get {
                return ResourceManager.GetString("Json", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /*--------------------------------------------------------------------------
        /// * linq.js - LINQ for JavaScript
        /// * ver 3.0.3-Beta4 (Oct. 9th, 2012)
        /// *
        /// * created and maintained by neuecc &lt;ils@neue.cc&gt;
        /// * licensed under MIT License
        /// * http://linqjs.codeplex.com/
        /// *------------------------------------------------------------------------*/
        ///
        ///(function (root, undefined) {
        ///    // ReadOnly Function
        ///    var Functions = {
        ///        Identity: function (x) { return x; },
        ///        True: function () { return tru [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string Linq {
            get {
                return ResourceManager.GetString("Linq", resourceCulture);
            }
        }
    }
}
