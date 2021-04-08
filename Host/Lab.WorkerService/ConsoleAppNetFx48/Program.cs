using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Topshelf;
using Topshelf.Extensions.Hosting;
using Host = Microsoft.Extensions.Hosting.Host;

namespace ConsoleAppNetFx48
{
    public class Program
    {
        private static void Main(string[] args)
        {
            var hostBuilder = CreateHostBuilder(args);

            var exitCode =
                hostBuilder.RunAsTopshelfService(config =>
                                                 {
                                                     var assemblyName = Assembly.GetEntryAssembly().GetName().Name;
                                                     config.SetServiceName(assemblyName);
                                                     config.SetDisplayName(assemblyName);
                                                     config.SetDescription("Runs a generic host as a Topshelf service.");
                                                     config.RunAsPrompt();
                                                 });
            Console.WriteLine($"服務控制狀態:{exitCode}");
            // hostBuilder.Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                       // .UseWindowsService()
                       .ConfigureServices((hostContext, services) => { services.AddHostedService<Worker>(); });
        }
    }
}