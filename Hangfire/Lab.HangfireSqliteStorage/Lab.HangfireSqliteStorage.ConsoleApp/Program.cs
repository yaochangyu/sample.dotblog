using System;
using Microsoft.Owin.Hosting;

namespace Lab.HangfireSqliteStorage.ConsoleApp
{
    internal class Program
    {
        private const  string      HOST_ADDRESS = "http://localhost:9527";
        private static IDisposable s_webApp;

        private static void Main(string[] args)
        {
            s_webApp = WebApp.Start<Startup>(HOST_ADDRESS);
            Console.WriteLine("Hangfire service start...");
            Console.ReadLine();
        }
    }
}