using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Spike.Network.Http;
using Spike.Collections;
using System.Xml.Serialization;
using System.Threading;
using System.IO;

namespace Spike.Box
{
    /// <summary>
    /// Represents a configuration of a website.
    /// </summary>
    [Serializable]
    [XmlRootAttribute("Configuration", Namespace = "", IsNullable = false)]
    public class AppConfig
    {
        #region Constructors
        /// <summary>
        /// Constructs a new instance of a web configuration.
        /// </summary>
        public AppConfig()
        {
            this.Rules = new AppUrlRewriteRuleCollection();
            this.Hosts = new AppHostNameCollection();
            this.Elements = new AppElementCollection();
        }

        /// <summary>
        /// Constructs a new instance of a web configuration.
        /// </summary>
        /// <param name="rules">The url rewrite rules</param>
        public AppConfig(params AppUrlRewriteRule[] rules) : this()
        {
            AddRule(rules);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the writable collection of host bindings for this website.
        /// </summary>
        [XmlArray("Hosts"), XmlArrayItem("Host", typeof(AppHostName))]
        public AppHostNameCollection Hosts
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the rules used by the Url Rewrite module
        /// </summary>
        [XmlArray("Rules"), XmlArrayItem("Rule", typeof(AppUrlRewriteRule))]
        public AppUrlRewriteRuleCollection Rules 
        { 
            get; 
            private set; 
        }

        /// <summary>
        /// Gets or sets the rules used by the Url Rewrite module
        /// </summary>
        [XmlArray("Elements"), XmlArrayItem("Element", typeof(AppElement))]
        public AppElementCollection Elements
        {
            get;
            private set;
        }
        #endregion

        #region Public Members
        /// <summary>
        /// Adds rewrite rules to the configuration.
        /// </summary>
        /// <param name="rules">The rules to add.</param>
        public void AddRule(params AppUrlRewriteRule[] rules)
        {
            foreach (var rule in rules)
                this.Rules.Add(rule);
        }

        /// <summary>
        /// Adds host names to the configuration.
        /// </summary>
        /// <param name="hosts">The hosts to add.</param>
        public void AddHost(params AppHostName[] hosts)
        {
            foreach (var host in hosts)
                this.Hosts.Add(host);
        }

        /// <summary>
        /// Adds host names to the configuration.
        /// </summary>
        /// <param name="hosts">The hosts to add.</param>
        public void AddHost(params string[] hosts)
        {
            foreach (var host in hosts)
                this.Hosts.Add(host);
        }

        /// <summary>
        /// Adds elements to the configuration.
        /// </summary>
        /// <param name="elements">The elements to add.</param>
        public void AddElement(params AppElement[] elements)
        {
            foreach (var element in elements)
                this.Elements.Add(element);
        }

        /// <summary>
        /// Gets the element by its key.
        /// </summary>
        /// <param name="key">The key of the element to retrieve.</param>
        /// <returns>The element if found, otherwise null.</returns>
        public AppElement GetElement(string key)
        {
            return this.Elements.Find(e => e.Template == key);
        }

        
        /// <summary>
        /// Gets whether the configuration has the element key or not.
        /// </summary>
        /// <param name="key">The key of the element to check.</param>
        /// <returns>The element if found, otherwise false.</returns>
        public bool HasElement(string key)
        {
            return this.Elements.Find(e => e.Template == key) != null;
        }
        #endregion

        #region Internal Members
        /// <summary>
        /// Gets the url rewrite rule for a particular path.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <param name="path">The path to get the rule for.</param>
        /// <returns>The rewrite rule.</returns>
        internal AppUrlRewriteRule GetRewrite(App site, string path)
        {
            // Make sure we have meta translations
            //this.EnsureMeta(site);

            // If it's a view, do not use URL rewriting
            if (path.Contains("view/"))
                return null;

            // Gets the match
            return GetMatch(path);
        }

        /// <summary>
        /// Gets the matching rewrite rule for the incoming path.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>If found, returns a rule, otherwise, returns null.</returns>
        private AppUrlRewriteRule GetMatch(string path)
        {
            foreach (var rule in this.Rules)
            {
                var match = rule.Regex.Match(path);
                if (match.Success)
                    return rule;
            }
            return null;
        }


        #endregion

        #region Save & Load
        /// <summary>
        /// Read/Write lock on the file
        /// </summary>
        private static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();

        /// <summary>
        /// Loads the specified file name.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        public static AppConfig Load(string fileName)
        {
            try
            {
                Lock.EnterReadLock();
                AppConfig config = null;

                // If the file does not exist, create a new one and save it.
                if (!File.Exists(fileName))
                    return new AppConfig();

                XmlSerializer serializer = new XmlSerializer(typeof(AppConfig));
                using (Stream stream = new FileStream(fileName, FileMode.Open))
                    config = serializer.Deserialize(stream) as AppConfig;

                if (config == null)
                    return new AppConfig();
                return config;
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Loads the specified configuration.
        /// </summary>
        /// <param name="site">The web site to save it within.</param>
        public static AppConfig Load(App site)
        {
            return AppConfig.Load(
                Path.Combine(site.LocalDirectory, "app.config.xml")
                );
        }

        /// <summary>
        /// Saves the specified file name.
        /// </summary>
        /// <param name="filename">Name of the file.</param>
        public void Save(string filename)
        {
            try
            {
                Lock.EnterWriteLock();

                // Save to the file
                XmlSerializer serializer = new XmlSerializer(typeof(AppConfig));
                using (Stream stream = new FileStream(filename, FileMode.Create))
                    serializer.Serialize(stream, this);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Saves the web config within the specified web site.
        /// </summary>
        /// <param name="site">The web site to save it within.</param>
        public void Save(App site)
        {
            this.Save(
                Path.Combine(site.LocalDirectory, "app.config.xml")
                );
        }

        #endregion

    }


}
