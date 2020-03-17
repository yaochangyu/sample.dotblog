using System;
using Microsoft.Owin.Hosting;

namespace ConsoleApp.OWIN.NET45
{
    internal class Program
    {
        private const string HOST_ADDRESS = "http://localhost:9527";

        private static void Main(string[] args)
        {
            using (var webApp = WebApp.Start<Startup>(HOST_ADDRESS))
            {
                Console.WriteLine("Web Api OWIN Start");
                Console.ReadKey();
            }
        }
    }
}