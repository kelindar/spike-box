using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Spike.Network.Http;
using System.IO.Compression;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Spike.Box
{
    public class HandlerResource : IAppHandler
    {
        /// <summary>
        /// A cache for CDN requests, allows to only query S3 once per type of file.
        /// </summary>
        private static ConcurrentDictionary<string, string> CdnCache 
            = new ConcurrentDictionary<string, string>();

        public bool CanHandle(App site, HttpContext context, string resource)
        {
            if (context.Request.HttpVerb != HttpVerb.Get)
                return false;

            if (!File.Exists(Path.Combine(site.LocalDirectory, resource)))
                return false;

            var info = new FileInfo(resource);
            if (site.Mime.GetMime(info.Extension) != null)
                return true;
            return false;
        }


        public void ProcessRequest(App site, HttpContext context, string resource, string query)
        {
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            var file = new FileInfo(Path.Combine(site.LocalDirectory, resource));
            var mime = site.Mime.GetMime(file.Extension);
            if (!file.Exists || mime == null || file.Name.EndsWith("web.config.xml"))
            {
                response.Status = "404";
                return;
            }

            // Check if we have dpr in the cookie
            var dprString = request.Cookies.GetString("dpr");

            // Check if we have dpr bigger than one in the cookie
            if (dprString != null && dprString != "1" && mime.StartsWith("image/"))
            {
                // Get the dpr value
                int dpr = 1;

                // Check if it's an image and if it exists
                if (Int32.TryParse(dprString, out dpr))
                {
                    // If dpr is bigger than one
                    if (dpr > 1)
                    {
                        var fullname = file.FullName;
                        var namebase = fullname.Remove(fullname.Length - file.Extension.Length);
                        var newfile = namebase + "@2x" + file.Extension;

                        // Set the new file to a 2x one
                        if (File.Exists(newfile))
                            file = new FileInfo(newfile);
                    }
                }
            }

            // Add Vary for javascript & css
            if (mime == "application/javascript" || mime == "application/x-javascript" || mime == "text/javascript" || mime == "text/css")
                response.Headers.Set("Vary", "Accept-Encoding");

            // Create a new resource from file and write to the response
            HttpResource content = new HttpResource(file);
            response.Status = "200";
            content.WriteTo(context);
         
        }


        public void OnRegister(App site)
        {
            site.DefaultPages.Add("index.html");
            site.DefaultPages.Add("index.htm");
        }

        public void OnUnregister(App site)
        {
            site.DefaultPages.Remove("index.html");
            site.DefaultPages.Remove("index.htm");
        }

    }


}
