using System;
using System.IO;
using System.Net;


namespace Spike.Box
{
	public class Program
	{
		public static void Main(string[] args)
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
