using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using System.IO;
using Spike.Network.Http;
using Spike.Providers;
using Spike.Collections;

namespace Spike.Box
{
    /// <summary>
    /// Represents a single web applications that handles all requests accordingly.
    /// </summary>
    public class App: MarshalByRefObject
    {
        private uint Oid;
        private List<IAppHandler> fHandlers;
        private bool fActive = false;
        private string fLocalDirectory;
        private string fVirtualDirectory;
        private HttpMimeMap fMime;
        private AppConfig fConfig;
        private AppFolder Folder;
        private AppScope fScope;

        #region Constructors
        public App(string virtualDirectory) : this(
            virtualDirectory,
            Path.Combine(AppServer.Current.AppRoot, virtualDirectory.StartsWith("/") ? virtualDirectory.Remove(0, 1) : virtualDirectory)){}
        public App(string virtualDirectory, string localDirectory)
        {
            this.fHandlers = new List<IAppHandler>();
            if (!virtualDirectory.StartsWith("/"))
                virtualDirectory = "/" + virtualDirectory;
            this.Mime = new HttpMimeMap();
            this.LocalDirectory = localDirectory;
            this.VirtualDirectory = virtualDirectory.StartsWith("/") ? virtualDirectory.ToLower() : "/" + virtualDirectory.ToLower();
            this.Folder = new AppFolder(this);
            this.DefaultPages = new List<string>();
            this.Active = true;

            // Generate the key
            this.Oid = this.GenerateKey();

            // Load the configuration from the local directory
            this.fConfig = AppConfig.Load(this);

            // Store the list of hosts in the AppDomain
            var hosts = fConfig.Hosts.ToArray();
            AppDomain.CurrentDomain.SetData("hosts", hosts);

            // Create a new scope and attach it to the context.
            var context = new ScriptContext();
            this.fScope = new AppScope(this, context);

            // Prepare new handlers
            var handlers = new IAppHandler[] {
                new HandlerResource(),
                new HandlerApp(),
                new HandlerView(),
                new HandlerElement(),
                new HandlerCss(),
                new HandlerJs()
            };

            // Register other handlers
            if (handlers != null && handlers.Length > 0)
            {
                for (int i = 0; i < handlers.Length; ++i)
                    this.Register(handlers[i]);
            }

            // Populate the repositories
            this.Folder.Invalidate();
        }

        /// <summary>
        /// Generates a secure, unique key for the application.
        /// </summary>
        /// <returns>The unique identifier of the application</returns>
        private uint GenerateKey()
        {
            return this.fVirtualDirectory.GetMurmurInt();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the unique key of the application.
        /// </summary>
        public uint Key
        {
            get { return this.Oid; }
        }

        /// <summary>
        /// Gets/sets whether the website is active or not
        /// </summary>
        public virtual bool Active
        {
            get { return fActive; }
            set { fActive = value; }
        }

        /// <summary>
        /// Gets the local (physical) path to the website
        /// </summary>
        public virtual string LocalDirectory
        {
            get { return fLocalDirectory; }
            protected set { fLocalDirectory = value; }
        }
        
        /// <summary>
        /// Gets the virtual directory of the website (e.g: www.mywebsite.com/myVirtualDirectory/
        /// </summary>
        public virtual string VirtualDirectory
        {
            get { return fVirtualDirectory; }
            protected set
            {
                fVirtualDirectory = value;
            }
        }
        
        /// <summary>
        /// Gets the map of Mime types handled by the website
        /// </summary>
        public virtual HttpMimeMap Mime
        {
            get { return fMime; }
            protected set { fMime = value; }
        }

        /// <summary>
        /// Gets the registered web handlers
        /// </summary>
        public virtual IViewCollection<IAppHandler> Handlers 
        {
            get { return new ReadOnlyList<IAppHandler>(fHandlers); }
        }

        /// <summary>
        /// Gets the configuration of this website.
        /// </summary>
        public AppConfig Configuration
        {
            get { return fConfig; }
        }

        /// <summary>
        /// Gets the scope of execution.
        /// </summary>
        public AppScope Scope
        {
            get { return fScope; }
        }

        /// <summary>
        /// Gets the script repository for this application.
        /// </summary>
        public MetaScriptStore Scripts
        {
            get { return Folder.Scripts; }
        }

        /// <summary>
        /// Gets the view repository for this application.
        /// </summary>
        public MetaViewStore Views
        {
            get { return Folder.Views; }
        }

        /// <summary>
        /// Gets the element repository for this application.
        /// </summary>
        public MetaElementStore Elements
        {
            get { return Folder.Elements; }
        }

        /// <summary>
        /// Gets the list of default pages to look for
        /// </summary>
        public virtual List<string> DefaultPages 
        { 
            get; 
            protected set; 
        }

        #endregion

        #region ProcessRequest Members
        /// <summary>
        /// Attempts to handle an incoming request from a separate AppDomain.
        /// </summary>
        public void ProcessRequest()
        {
            var domain = AppDomain.CurrentDomain;
            var context = domain.GetData("context") as HttpContext;

            // Check whether we can handle it and then process it.
            ProcessRequest(context);
        }

        /// <summary>
        /// Performs the request processing. This code is executed within a protected AppDomain.
        /// </summary>
        /// <param name="context">The http context to process.</param>
        public virtual void ProcessRequest(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            if (!this.Active)
                throw new HttpForbiddenException();

            HttpRequest request = context.Request;
            HttpResponse response = context.Response;
            HttpVerb verb = context.Request.HttpVerb;
            string path = context.Request.Path;
            string page;
            string query;

            // Attempt to rewrite
            var rule = fConfig.GetRewrite(this, path);
            if (rule != null)
            {
                // Rewrite the path
                var rewritten = rule.Regex.Replace(path, rule.Destination);
                switch (rule.Type)
                {
                    case AppUrlRewriteType.Default: path = rewritten; break;
                    case AppUrlRewriteType.Http301: context.Response.RedirectPermanent(rewritten); break;
                    case AppUrlRewriteType.Http302: context.Response.Redirect(rewritten); break;
                }
            }


            // get local path
            path = path.StartsWith(this.VirtualDirectory)
                ? path.Remove(0, this.VirtualDirectory.Length + 1)
                : path.Remove(0, 1);

            HttpUtility.ParseUrl(path, out page, out query);
            if (String.IsNullOrEmpty(page))
            {
                for (int i = DefaultPages.Count - 1; i >= 0; --i)
                {
                    string index = Path.Combine(this.LocalDirectory, DefaultPages[i]);
                    if (File.Exists(index))
                    {
                        page = index;
                        break;
                    }
                }
            }

            if (!String.IsNullOrEmpty(page))
            {
                for (int i = fHandlers.Count - 1; i >= 0; --i)
                {
                    IAppHandler handler = fHandlers[i];
                    if (handler.CanHandle(this, context, page))
                    {
                        handler.ProcessRequest(this, context, page, query);
                        return;
                    }
                }
            }

            response.Status = "404";
            return;
        }

        #endregion

        #region AppHandlers Registration
        /// <summary>
        /// Registers a Web handler to handle a particular type of resource
        /// </summary>
        /// <param name="handler">Web handler to register</param>
        public void Register(IAppHandler handler)
        {
            if (!Handlers.Contains(handler))
            {
                handler.OnRegister(this);
                fHandlers.Add(handler);
            }
        }

        /// <summary>
        /// Unregisters a web handler that handles a particular type of resource
        /// </summary>
        public void Unregister(IAppHandler handler)
        {
            if (Handlers.Contains(handler))
            {
                handler.OnUnregister(this);
                fHandlers.Remove(handler);
            }
        }
        #endregion


    }

    #region IWebHandler
    /// <summary>
    /// Represents an HTTP handler for a particular application.
    /// </summary>
    public interface IAppHandler
    {
        /// <summary>
        /// Checks whether the handler can handle an incoming request or not
        /// </summary>
        /// <param name="context">An HttpContext object that provides references to the intrinsic
        /// server objects (for example, Request, Response, Session, and Server) used
        /// to service HTTP requests.</param>
        /// <param name="site">Website which handles the request</param>
        /// <param name="url">Local resource (page name)</param>
        /// <returns></returns>
        bool CanHandle(App site, HttpContext context, string resource);

        /// <summary>
        /// Processes a web request for a given url
        /// </summary>
        /// <param name="site">Website which handles the request</param>
        /// <param name="context">An HttpContext object that provides references to the intrinsic
        /// server objects (for example, Request, Response, Session, and Server) used
        /// to service HTTP requests.</param>
        /// <param name="resource">Resource (e.g: page name) to access</param>
        /// <param name="query">Query parameters</param>
        void ProcessRequest(App site, HttpContext context, string resource, string query);

        /// <summary>
        /// Invoked when the web handler is registered to an application.
        /// </summary>
        void OnRegister(App site);

        /// <summary>
        /// Invoked when the web handler is unregistered from an application.
        /// </summary>
        void OnUnregister(App site);

    }
    #endregion
}
