using System;
using Microsoft.Owin.Hosting;

namespace OWinHost.AspNet48.WebApi
{
    internal class Program
    {
        private const string HOST_ADDRESS = "http://localhost:9527";

        private static void Main(string[] args)
        {
            var webApp = WebApp.Start<Startup>(HOST_ADDRESS);
            Console.WriteLine($"伺服器已啟動, 位置：{HOST_ADDRESS}");
            Console.WriteLine("按下任意建離開應用程式");
            Console.ReadLine();
            webApp.Dispose();
        }
    }
}
