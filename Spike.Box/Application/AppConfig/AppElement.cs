using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Spike.Network.Http;
using Spike.Collections;
using System.Xml.Serialization;

namespace Spike.Box
{
    /// <summary>
    /// Represents an element (component) configuration.
    /// </summary>
    [Serializable]
    public class AppElement
    {
        /// <summary>
        /// Constructs a new rule.
        /// </summary>
        public AppElement()
        {

        }

        /// <summary>
        /// Constructs a new configuration for an element (component).
        /// </summary>
        /// <param name="name">The name of the element</param>
        /// <param name="template">The template of the component.</param>
        /// <param name="transclude">Whether the element should wrap other elements</param>
        public AppElement(string name, string template, bool transclude)
        {
            this.Name = name;
            this.Template = template;
            this.Transclude = transclude;
        }

        /// <summary>
        /// Gets or sets the name of the element.
        /// </summary>
        [XmlAttribute]
        public string Name
        {
            get;
            set;
        }


        /// <summary>
        /// Gets or sets the template to use for the component.
        /// </summary>
        [XmlAttribute]
        public string Template
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets whether the element should wrap other elements.
        /// </summary>
        [XmlAttribute]
        public bool Transclude
        {
            get;
            set;
        }

    }

    /// <summary>
    /// Defines a collection of rewrite rules
    /// </summary>
    [Serializable]
    public sealed class AppElementCollection : ConcurrentList<AppElement>
    {

    }
}
