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
    /// Represents a rewrite rule for various URLs of a website.
    /// </summary>
    [Serializable]
    public class AppUrlRewriteRule
    {
        [XmlIgnore]
        private string Pattern;

        /// <summary>
        /// Constructs a new rule.
        /// </summary>
        public AppUrlRewriteRule()
        {

        }

        /// <summary>
        /// Constructs a new rule.
        /// </summary>
        /// <param name="source">The source regex.</param>
        /// <param name="destination">The destination resource.</param>
        public AppUrlRewriteRule(string source, string destination)
        {
            this.Source = source;
            this.Destination = destination;
        }

        /// <summary>
        /// Gets or sets the source regular expression.
        /// </summary>
        [XmlIgnore]
        public Regex Regex
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the source regular expression.
        /// </summary>
        [XmlAttribute]
        public string Source
        {
            get { return this.Pattern; }
            set
            {
                this.Pattern = value;
                this.Regex = new Regex(this.Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
        }

        /// <summary>
        /// Gets or sets the destination regular expression.
        /// </summary>
        [XmlAttribute]
        public string Destination
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the type of the rewrite.
        /// </summary>
        [XmlAttribute]
        public AppUrlRewriteType Type
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Represents the type of the rewrite
    /// </summary>
    public enum AppUrlRewriteType
    {
        /// <summary>
        /// Default rewrite
        /// </summary>
        Default,

        /// <summary>
        /// Http Permanent Redirect
        /// </summary>
        Http301,

        /// <summary>
        /// Http Temporary Redirect
        /// </summary>
        Http302
    }

    /// <summary>
    /// Defines a collection of rewrite rules
    /// </summary>
    [Serializable]
    public sealed class AppUrlRewriteRuleCollection : ConcurrentList<AppUrlRewriteRule>
    {

    }
}
