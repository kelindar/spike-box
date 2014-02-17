using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Spike.Network.Http;
using System.Collections.Concurrent;
using Spike.Text;

namespace Spike.Box
{
    public class HandlerCss : IAppHandler
    {
        private ConcurrentDictionary<string, DateTime> TimeMap =
            new ConcurrentDictionary<string, DateTime>();

        private ConcurrentDictionary<string, HttpResource> Cache =
            new ConcurrentDictionary<string, HttpResource>();

        public bool CanHandle(App site, HttpContext context, string path)
        {
            if (context.Request.HttpVerb != HttpVerb.Get)
                return false;

            if (!path.EndsWith("styles.min.css"))
                return false;

            // Refresh the cache
            this.Refresh(site, path);
            return this.Cache.ContainsKey(path);
        }


        public void ProcessRequest(App site, HttpContext context, string resource, string query)
        {
            var response = context.Response;
            HttpResource content;
            if (Cache.TryGetValue(resource, out content))
            {
                // Prepare the response
                response.Status = "200";
                response.ContentType = "text/css";
                response.Headers.Set("Vary", "Accept-Encoding");

                // Write the css content
                content.WriteTo(context);
            }
            else
            {
                // CSS not found in the cache, should not get here.
                response.Status = "404";
            }
        }

        public void OnRegister(App site)
        {

        }

        public void OnUnregister(App site)
        {
            
        }

        private void Refresh(App app, string path)
        {
            var cssFolder = Path.Combine(app.LocalDirectory, "css");
            if (!Directory.Exists(cssFolder))
                Directory.CreateDirectory(cssFolder);

            var cssFiles = Directory
                .GetFiles(cssFolder)
                .Where(f => f.EndsWith(".css"));

            if (cssFiles
                .Where(f => !TimeMap.ContainsKey(f) || File.GetLastWriteTimeUtc(f) != TimeMap[f])
                .Any())
            {
                // Combine CSS
                var combinedCss = cssFiles
                    .OrderBy(f => f.Substring(0, f.Length-4))
                    .Select(f => File.ReadAllText(f))
                    .Where(t => !t.StartsWith("/* Auto-Combine: Exclude", StringComparison.InvariantCultureIgnoreCase))
                    .Aggregate((a, b) => a + Environment.NewLine + b);

                // Minify CSS
                var cachedCss = new HttpResource( 
                    DateTime.UtcNow,
                    Encoding.UTF8.GetBytes(
                        new Minifier().MinifyStyleSheet(combinedCss)
                        ), "text/css");

                // Update write times
                foreach (var cssFile in cssFiles)
                    TimeMap.AddOrUpdate(cssFile, File.GetLastWriteTimeUtc(cssFile),
                        (f, d) => File.GetLastWriteTimeUtc(f));

                // Add or update the combined css.
                this.Cache.AddOrUpdate(path, cachedCss, (s, o) => cachedCss);
            }
        }
    }
}
