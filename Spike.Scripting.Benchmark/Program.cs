using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Diagnostics;

namespace Benchmarks
{
    class Program
    {
        static string GetExecutableDirectory()
        {
            return Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
        }

        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.Priority =
                System.Threading.ThreadPriority.Highest;

            var basePath = new DirectoryInfo(GetExecutableDirectory()).Parent.Parent.FullName;

            TestSuite sunSpider = new SunSpiderTestSuite(basePath);
            sunSpider.Run();

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
    }
}
