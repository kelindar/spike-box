using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Spike.Providers;
using Spike.Collections;
using Spike.Network.Http;
using System.Reflection;
using System.Collections.Concurrent;
using System.Net;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Spike.Box
{
    /// <summary>
    /// Represents a web server itself.
    /// </summary>
    public sealed class AppServer : IHttpHandler
    {
        #region Singleton Implementation
        private static volatile AppServer instance;
        private static object Lock = new Object();

        /// <summary>
        /// Private constructor for the singleton.
        /// </summary>
        private AppServer() { }

        /// <summary>
        /// Gets the current <see cref="AppServer"/> instance.
        /// </summary>
        public static AppServer Current
        {
            get
            {
                if (instance == null)
                {
                    lock (Lock)
                    {
                        if (instance == null)
                            instance = new AppServer();

                        // Set a default folder
                        instance.AppRoot = Path.Combine(Service.BaseDirectory, "Apps" + Path.DirectorySeparatorChar);
                    }
                }

                return instance;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Gets the web sites currently registered on the server.
        /// </summary>
        private ConcurrentList<AppRegistration> Applications = new ConcurrentList<AppRegistration>();
        private ConcurrentDictionary<string, AppRegistration> IndexByName = new ConcurrentDictionary<string, AppRegistration>();
        private ConcurrentDictionary<uint, AppRegistration> IndexByKey = new ConcurrentDictionary<uint, AppRegistration>();

        /// <summary>
        /// Initializes the web server instance.
        /// </summary>
        [InvokeAt(InvokeAtType.Initialize)]
        public static void Initialize()
        {
            // Create a new web server and register it as an HTTP Handler.
            Service.Http.Register(AppServer.Current);

            // Scan the web root
            AppServer.Current.ScanAppRoot();
        }

        #endregion

        #region Public Properties
        private string AppFolder;
        private string AppEndpoint;

        /// <summary>
        /// Gets the root folder for the web directory.
        /// </summary>
        public string AppRoot
        {
            get { return this.AppFolder; }
            set
            {
                // Set the folder
                this.AppFolder = value;

                // Ensure we have a directory
                if (!Directory.Exists(this.AppFolder))
                    Directory.CreateDirectory(this.AppFolder);
            }
        }

        /// <summary>
        /// Gets or sets the endpoint that can be used to access this application server.
        /// </summary>
        public string Endpoint
        {
            get 
            { 
                if(AppEndpoint == null)
                    this.AppEndpoint = Network.NetConfig.PublicHttpEndpoints.FirstOrDefault();

                return this.AppEndpoint; 
            }
            set { this.AppEndpoint = value; }
        }

        #endregion

        #region Registration Members
        /// <summary>
        /// Scans the application root and registers all applications found in the directory.
        /// </summary>
        private void ScanAppRoot()
        {
            // Web root must be there.
            if (!Directory.Exists(AppRoot))
                return;

            // For each directory in the root, make sure we have a website
            foreach (var directory in Directory
                .GetDirectories(AppRoot)
                .Select(d => new DirectoryInfo(d)))
            {
                Register(directory.Name);
            }
        }


        /// <summary>
        /// Registers a website or marks it to be registered when server starts
        /// </summary>
        /// <param name="app">The application to register.</param>
        /// <param name="appDomain">The AppDomain the website is executing in.</param>
        public void Register(App app, AppDomain appDomain)
        {
            // Create a new registration
            var registration = new AppRegistration(app, appDomain);

            // Add the site to the local registry
            this.Applications.Add(registration);

            // Add to the index by key
            this.IndexByKey.AddOrUpdate(app.Key, registration, (k, o) => registration);
        }

        /// <summary>
        /// Registers a new website or marks it to be registered when server starts
        /// </summary>
        public void Register(string virtualDirectory)
        {
            // Get or create a web site instance
            var registration = GetByVirtualDirectory(virtualDirectory);
            if (registration != null)
                return; // Already registered

#if APP_DOMAIN
            // Create an AppDomain and an instance of a web site inside
            var appDomain = AppDomain.CreateDomain(virtualDirectory);
            var typeFlags = (BindingFlags.Public | BindingFlags.CreateInstance | BindingFlags.Instance);
            var webSite = appDomain.CreateInstanceAndUnwrap("Spike.Web", "Spike.Web.WebSite", false, typeFlags, null, new object[] { virtualDirectory }, null, null) as WebSite;
#else
            // Create a new site in the current app domain
            var appDomain = AppDomain.CurrentDomain;
            var webSite = new App(virtualDirectory);
#endif

            // Forward the registration
            this.Register(webSite, appDomain);
        }
        #endregion

        #region Retrieval Members
        /// <summary>
        /// Attempts to get a registered application by the virtual directory.
        /// </summary>
        /// <param name="path">The virtual directory.</param>
        /// <returns>The registration or null if no registration matching the virtual directory was found.</returns>
        internal AppRegistration GetByVirtualDirectory(string path)
        {
            // Span the registry
            foreach (var registration in this.Applications)
            {
                // Check if the path starts with virtual directory
                if (path.StartsWith(registration.VirtualDirectory))
                    return registration;
            }

            return null;
        }

        /// <summary>
        /// Attempts to get a registered application by the referrer (debug only).
        /// </summary>
        /// <param name="referrer">The http referrer.</param>
        /// <returns>The registration or null if no registration matching the virtual directory was found.</returns>
        internal AppRegistration GetByReferrer(string referrer)
        {
            // Span the registry
            foreach (var registration in this.Applications)
            {
                // Check if the path starts with virtual directory
                if (referrer.Contains(registration.VirtualDirectory))
                    return registration;
            }

            return null;
        }

        /// <summary>
        /// Attempts to get a registered application by the application key.
        /// </summary>
        /// <param name="key">The key of the application.</param>
        /// <returns>The registration or null if no match was found.</returns>
        internal AppRegistration GetByKey(uint key)
        {
            AppRegistration registration;
            if (this.IndexByKey.TryGetValue(key, out registration))
                return registration;
            return null;
        }
        

        /// <summary>
        /// Attempts to get a registered application by the host header.
        /// </summary>
        /// <param name="host">The host header value.</param>
        /// <returns>The registration or null if no match was found.</returns>
        internal AppRegistration GetByHost(string host)
        {
            // Normalize by putting the host into lowercase
            // and stripping out the port number.
            host = new Regex(@":\d+")
                .Replace(host.ToLower(), "");

            // Result
            AppRegistration registration;

            // Retrieve or add the web app
            if (this.IndexByName.TryGetValue(host, out registration))
                return registration;

            // Scan the entire registry
            foreach (var site in this.Applications)
            {
                // Match predefined endpoints
                foreach (var rule in site.Hosts)
                {
                    // Found a match, add to cache and return
                    if (rule.Rule.IsMatch(host))
                    {
                        this.IndexByName.TryAdd(host, site);
                        return site;
                    }
                }
            }

            // None matched by host
            return null;
        }

        /// <summary>
        /// Attempts to get a registered website by the HttpContext.
        /// </summary>
        /// <param name="context">The HttpContext value.</param>
        /// <returns>The registration or null if no match was found.</returns>
        internal AppRegistration GetByContext(HttpContext context)
        {
            // Prepare the result
            AppRegistration result = null;
            bool isAddress = false;

            // Check if we have a bound host name
            var host = context.Request.Host;
            if (host != null)
            {
                // Parse the address
                IPAddress address;
                isAddress = IPAddress.TryParse(host, out address);

                // This is not an ip address, return the web by host
                if (!isAddress)
                    result = GetByHost(context.Request.Host);
            }

            // If we found by host, return it
            if (result != null)
                return result;

            // Check by virtual directory
            result = GetByVirtualDirectory(context.Request.Path);
            if (result != null)
                return result;

            // Debug only, check by referrer
            if (isAddress || Debugger.IsAttached)
            {
                // Get by referrer
                result = GetByReferrer(context.Request.Referer);

                // If we found by host, return it
                if (result != null)
                    return result;
            }

            // Not found
            return null;
        }
        #endregion

        #region IHttpHandler Members
        /// <summary>
        /// Gets whether the request can be handled or not.
        /// </summary>
        /// <param name="context">The context of the request.</param>
        /// <param name="verb">The HTTP verb of the request.</param>
        /// <param name="url">The url of the request.</param>
        /// <returns>Whether the request can be handled or not.</returns>
        public bool CanHandle(HttpContext context, HttpVerb verb, string url)
        {
            // Ignore, do not handle specific paths
            if (url.StartsWith("/app") ||
                url.StartsWith("/app.config.xml") ||
                url.StartsWith("/status") ||
                url.StartsWith("/live-ajax") ||
                url.StartsWith("/live-check") ||
                url.StartsWith("/socket.io"))
                return false;

            // Check if we have a bound host name
            return GetByContext(context) != null;
        }

        /// <summary>
        /// Performs the request processing. 
        /// </summary>
        /// <param name="context">The http context to process.</param>
        public void ProcessRequest(HttpContext context)
        {
            // Get by context
            var registration = GetByContext(context);
            if (registration == null)
                return;

#if APP_DOMAIN
            // AppDomain call
            var target = registration.AppDomain;
            target.SetData("context", context);
            target.DoCallBack(registration.WebSite.ProcessRequest);
#else
            // Process a request in the same app domain
            registration.Application.ProcessRequest(context);
#endif
        }


        #endregion
    }

    #region AppRegistration
    /// <summary>
    /// Represents a registered <see cref="Application"/>.
    /// </summary>
    internal sealed class AppRegistration
    {
        /// <summary>
        /// Constructs a new registration.
        /// </summary>
        /// <param name="site">The <see cref="Application"/>.</param>
        /// <param name="domain">The <see cref="AppDomain"/>.</param>
        public AppRegistration(App site, AppDomain domain)
        {
            this.Application = site;
            this.AppDomain = domain;
            this.VirtualDirectory = site.VirtualDirectory;
            this.Hosts = this.AppDomain.GetData("hosts") as AppHostName[];
        }

        /// <summary>
        /// Gets the registered application.
        /// </summary>
        public readonly App Application;

        /// <summary>
        /// Gets the virtual directory of the website.
        /// </summary>
        public readonly string VirtualDirectory;

        /// <summary>
        /// Gets the execution context.
        /// </summary>
        public readonly AppDomain AppDomain;

        /// <summary>
        /// Gets the hosts.
        /// </summary>
        public readonly AppHostName[] Hosts;
    }
    #endregion
}
