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
        #region Template Loading
        private string AppTemplate = null;
        private string AppTemplatePath = null;
        private DateTime AppTemplateDate;

        /// <summary>
        /// Gets the html source for the application page.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        private string GetHtml(App app)
        {
            if (AppTemplate != null)
            {
                // If we have a new version on disk, load it
                if (this.AppTemplateDate < File.GetLastWriteTimeUtc(this.AppTemplatePath))
                    this.AppTemplate = File.ReadAllText(this.AppTemplatePath);
                return this.AppTemplate;
            }

            
            // Try to get the template from the application directory
            this.AppTemplatePath = Path.GetFullPath(
                Path.Combine(app.LocalDirectory, "index.ng")
                );

            // Check if we have a file there
            if (!File.Exists(this.AppTemplatePath))
            {
                // No file, create one using the embedded template
                Service.Logger.Log(LogLevel.Warning, "Application template 'index.ng' not found, creating one...");
                File.WriteAllText(this.AppTemplatePath, Resources.app);

                // Load the template too
                this.AppTemplate = Resources.app;
            }
            else
            {
                // If we have a new version on disk, load it
                this.AppTemplate = File.ReadAllText(this.AppTemplatePath);
            }

            // Return the template we have
            return this.AppTemplate;
        }
        #endregion

        public bool CanHandle(App site, HttpContext context, string path)
        {
            if (context.Request.HttpVerb != HttpVerb.Get)
                return false;

            if (!path.StartsWith("app/"))
                return false;

            // Only able to handle index.ng
            return true;
        }


        public void ProcessRequest(App app, HttpContext context, string resource, string query)
        {
            // Get the response and the app
            var response = context.Response;
            var view = "/view/" + resource.Substring(4);

            
            // Change content dynamically
            var content = this.GetHtml(app)
                .Replace("{{host}}", AppServer.Current.Endpoint)
                .Replace("{{app}}", app.Key.ToString())
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
