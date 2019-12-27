using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;

namespace Lab.HangfireApp
{
    class Program
    {
        private static IDisposable s_webApp;
        private const string HOST_ADDRESS = "https://localhost:44392";
        //private const string HOST_ADDRESS = "http://localhost:8001";
        static void Main(string[] args)
        {
            s_webApp = WebApp.Start<Startup>(HOST_ADDRESS);
            Console.ReadLine();
        }
    }
}
