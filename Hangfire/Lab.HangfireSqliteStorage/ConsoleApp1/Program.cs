using System;
using Microsoft.Owin.Hosting;

namespace ConsoleApp1
{
    class Program
    {
        private static IDisposable s_webApp;
        private const  string      HOST_ADDRESS = "http://localhost:9527";
        static void Main(string[] args)
        {
            s_webApp = WebApp.Start<Startup>(HOST_ADDRESS);
            Console.WriteLine("Hangfire service start...");
            Console.ReadLine();
        }
    }
}
