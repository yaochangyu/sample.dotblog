using System;
using System.Web.Http;
using System.Web.Http.SelfHost;

namespace Lab.SelfHost.NET4
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var config = new HttpSelfHostConfiguration("http://localhost:9527");

            config.Routes.MapHttpRoute(
                                       "API Default", "api/{controller}/{id}",
                                       new {id = RouteParameter.Optional});

            using (var server = new HttpSelfHostServer(config))
            {
                server.OpenAsync().Wait();
                Console.WriteLine("Press Enter to quit.");
                Console.ReadLine();
            }
        }
    }
}