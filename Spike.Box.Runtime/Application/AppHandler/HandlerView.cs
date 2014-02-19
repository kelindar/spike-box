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
    /// Represents a handler for the view template.
    /// </summary>
    public class HandlerView : IAppHandler
    {
        public bool CanHandle(App site, HttpContext context, string path)
        {
            if (context.Request.HttpVerb != HttpVerb.Get)
                return false;

            if (!path.StartsWith("view/"))
                return false;

            // Only able to handle views
            return true;
        }


        public void ProcessRequest(App site, HttpContext context, string resource, string query)
        {
            const int prefixLen = 5; //sizeof("view/")

            // Get the response and the view
            var response = context.Response;
            var key = resource.Substring(prefixLen);
            MetaView view;

            // Get the view from the repository
            if (site.Views.TryGet(key, out view))
            {
                // Write the response
                response.Status = "200";
                response.ContentType = "text/html";
                response.Write(view.Template);
            }
            else
            {
                // Not found
                response.Status = "404";
            }
        }


        /// <summary>
        /// Creates and sends an HttpResponse packet for an exception, wraps it to an error 401
        /// </summary>
        private void Send412(HttpResponse response)
        {
            response.Status = "412";
            response.Write("<html><head><title>412 Precondition Failed</title></head><body>");
            response.Write("<h3>Precondition Failed (412)</h3>");
            response.Write("Unable to find a necessary cookie, please ensure that your browser supports cookies and have them enabled.");
            response.Write("</body></html>");
        }

        public void OnRegister(App site)
        {

        }

        public void OnUnregister(App site)
        {
            
        }


    }
}
