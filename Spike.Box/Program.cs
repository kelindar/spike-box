using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Spike.Box
{
    class Program
    {
        static void Main(string[] args)
        {
            // Set some defaults...
            AppServer.Current.AppRoot = new DirectoryInfo("../../../Spike.Apps/").FullName;
            //AppServer.Current.Endpoint = "127.0.0.1:8080";

            // Run the server on the specified endpoints
            Service.Listen(
                new TcpBinding(IPAddress.Any, 8002),
                new TcpBinding(IPAddress.Any, 8080),
                new TcpBinding(IPAddress.Any, 80)
                );

        }
    }
}
