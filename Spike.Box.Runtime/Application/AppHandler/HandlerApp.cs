using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Spike.Network.Http;
using System.Collections.Concurrent;
using Spike.Box.Properties;

namespace Spike.Box
{
    /// <summary>
    /// Represents a handler for the index page.
    /// </summary>
    public class HandlerApp : IAppHandler
    {
        public bool CanHandle(App site, HttpContext context, string path)
        {
            if (context.Request.HttpVerb != HttpVerb.Get)
                return false;

            if (!path.StartsWith("app/"))
                return false;

            // Only able to handle index.ng
            return true;
        }


        public void ProcessRequest(App site, HttpContext context, string resource, string query)
        {
            // Get the response and the app
            var response = context.Response;
            var view = "/view/" + resource.Substring(4);
            var app  = Resources.app;
            
            // Change content dynamically
            var content = app
                .Replace("{{host}}", AppServer.Current.Endpoint)
                .Replace("{{app}}", site.Key.ToString())
                .Replace("{{view}}",  view)
                .Replace("{{title}}", "Spike.Box");
                

            // Write the response
            response.Status = "200";
            response.ContentType = "text/html";
            response.Write(content);
        }

        public void OnRegister(App site)
        {

        }

        public void OnUnregister(App site)
        {
            
        }


    }
}
