using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Spike.Network.Http;
using System.Collections.Concurrent;
using System.IO.Compression;
using Spike.Text;

namespace Spike.Box
{
    public class HandlerJs : IAppHandler
    {
        private ConcurrentDictionary<string, HttpResource> Cache =
            new ConcurrentDictionary<string, HttpResource>();

        public bool CanHandle(App site, HttpContext context, string path)
        {
            // Only HTTP GET requests
            if (context.Request.HttpVerb != HttpVerb.Get)
                return false;

            // Only handles minified javascript
            if (!path.EndsWith(".min.js"))
                return false;

            // If a file exists, do not handle with this handler
            if (File.Exists(Path.Combine(site.LocalDirectory, path)))
                return false;

            // Ok
            return true;
        }


        public void ProcessRequest(App site, HttpContext context, string resource, string query)
        {
            var request = context.Request;
            var response = context.Response;

            // Spike Box source should be composed from built-in stuff
            if (resource.StartsWith("js/spike.box.min.js"))
            {
                // Prepare the response
                response.Status = "200";
                response.Headers.Set("Vary", "Accept-Encoding");

                // Write the script to the context
                AppClient.Script.WriteTo(context);
                return;
            }

            // Construct the name of the javascript file
            var jsfile = Path.Combine(site.LocalDirectory, resource.Replace(".min.js", ".js"));
            if (!File.Exists(jsfile))
            {
                // Javascript file does not exit, return a 404
                response.Status = "404";
                return;
            }

            // We have a valid version, just get this and
            HttpResource content;
            if (!this.Cache.TryGetValue(jsfile, out content) || content.LastWriteUtc != File.GetLastWriteTimeUtc(jsfile))
            {
                // Last write not found or outdated, read and minify
                content = new HttpResource(
                    File.GetLastWriteTimeUtc(jsfile),
                    Encoding.UTF8.GetBytes(
                        new Minifier().MinifyJavaScript(File.ReadAllText(jsfile))
                    ), "application/javascript");

                // Update the cache
                this.Cache.AddOrUpdate(jsfile, content, (s, c) => content);
            }
            
            // Prepare the response
            response.Status = "200";
            response.Headers.Set("Vary", "Accept-Encoding");

            // Write to the context
            content.WriteTo(context);

        }

        public void OnRegister(App site)
        {

        }

        public void OnUnregister(App site)
        {
            
        }

    }
}
