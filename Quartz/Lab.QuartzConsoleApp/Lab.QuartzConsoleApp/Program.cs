using System;
using Microsoft.Owin.Hosting;

namespace Lab.QuartzConsoleApp
{
    internal class Program
    {
        private const  string      HOST_ADDRESS = "https://localhost:44377";
        private static IDisposable s_webApp;

        private static void Main(string[] args)
        {
            s_webApp = WebApp.Start<Startup>(HOST_ADDRESS);
            Console.WriteLine($"{HOST_ADDRESS}, already start!!!");
            Console.ReadLine();
        }
    }
}