using System;
using Nancy.Hosting.Self;

namespace Lab.ServiceNet4
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var baseUri = new Uri("http://localhost:9527");
            using (var host = new NancyHost(baseUri))
            {
                host.Start();
                Console.WriteLine("Service created");
                Console.WriteLine("Press any key to stop...");
                Console.Read();
                host.Stop();
            }
        }
    }
}