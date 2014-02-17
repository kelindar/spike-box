using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Spike.Collections;

namespace Spike.Box
{
    /// <summary>
    /// Defines a host name binding.
    /// </summary>
    [Serializable]
    public class AppHostName
    {
        [XmlIgnore]
        private string HostNameString;

        /// <summary>
        /// Constructs a new instance of a host name binding.
        /// </summary>
        public AppHostName()
        {

        }

        /// <summary>
        /// Constructs a new instance of a host name binding.
        /// </summary>
        /// <param name="wildCardName">The wildcard rule.</param>
        public AppHostName(string wildCardName)
	    {
            this.Name = wildCardName;
	    }

        /// <summary>
        /// Gets the host name rule to match.
        /// </summary>
        [XmlIgnore]
        public Regex Rule
        { 
            get; 
            set;
        }

        /// <summary>
        /// Gets the host name rule to match.
        /// </summary>
        [XmlAttribute]
        public string Name
        {
            get { return this.HostNameString; }
            set
            {
                try
                {
                    this.Rule = new Regex("^" + Regex.Escape(value)
                        .Replace("\\*", ".*")
                        .Replace("\\?", ".") + "$", RegexOptions.Compiled);

                    // Set the new name
                    this.HostNameString = value;
                }
                catch {}
            }
        }

    }


    /// <summary>
    /// Defines a host name bindings collection
    /// </summary>
    [Serializable]
    public sealed class AppHostNameCollection : ConcurrentList<AppHostName>
    {
        /// <summary>
        /// Adds the hostname in the wildcard mode.
        /// </summary>
        public void Add(string hostName)
        {
            base.Add(new AppHostName(hostName));
        }

    }
}
