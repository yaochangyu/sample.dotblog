using System;
using Microsoft.Owin.Hosting;

namespace WebApiOwinNet48
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            using (WebApp.Start<Startup>(ServerSetting.HostEndpoint))
            {
                Console.WriteLine($"伺服器已啟動, 位置：{ServerSetting.HostEndpoint}");
                Console.WriteLine("按下任意建離開應用程式");
                Console.ReadLine();
            }
        }
    }
}