using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Spike.Network.Http;
using Spike.Text;

namespace Spike.Box
{
    /// <summary>
    /// Represents the client related factory.
    /// </summary>
    internal static class AppClient
    {
        private static HttpResource Source = null;

        /// <summary>
        /// Gets the script that is used to link the client to the server.
        /// </summary>
        public static HttpResource Script
        {
            get 
            {
                if (AppClient.Source == null)
                {
                    // Load all the sources
                    var source = new StringBuilder();
                    source.AppendLine(AppClient.Load("Spike.Box.Application.AppClient.angular.js"));
                    source.AppendLine(AppClient.Load("Spike.Box.Application.AppClient.angular-animate.js"));
                    source.AppendLine(AppClient.Load("Spike.Box.Application.AppClient.angular-cookies.js"));
                    source.AppendLine(AppClient.Load("Spike.Box.Application.AppClient.angular-loader.js"));
                    source.AppendLine(AppClient.Load("Spike.Box.Application.AppClient.angular-resource.js"));
                    source.AppendLine(AppClient.Load("Spike.Box.Application.AppClient.angular-route.js"));
                    source.AppendLine(AppClient.Load("Spike.Box.Application.AppClient.angular-sanitize.js"));
                    source.AppendLine(AppClient.Load("Spike.Box.Application.AppClient.angular-touch.js"));
                    source.AppendLine(AppClient.Load("Spike.Box.Application.AppClient.spike-sdk.min.js"));
                    source.AppendLine(AppClient.Load("Spike.Box.Application.AppClient.spike-cache.js"));
                    source.AppendLine(AppClient.Load("Spike.Box.Application.AppClient.spike-box.js"));
                    

                    DateTime lastWriteUtc = DateTime.UtcNow;

                    AppClient.Source = new HttpResource(
                        lastWriteUtc,
                        Encoding.UTF8.GetBytes(
                            source.ToString()
                            //new Minifier().MinifyJavaScript(source.ToString())
                        ), "application/javascript");
                }

                // Return the source we have.
                return AppClient.Source;
            }
        }

        /// <summary>
        /// Loads a particular resource.
        /// </summary>
        /// <param name="resourceName">The name of an embedded resource to load.</param>
        /// <returns>The string content.</returns>
        private static string Load(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
