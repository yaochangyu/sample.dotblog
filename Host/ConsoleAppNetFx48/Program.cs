using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConsoleAppNetFx48
{
    // internal class Program1
    // {
    //     private static void Main(string[] args)
    //     {
    //         var hostBuilder = Host.CreateDefaultBuilder(args)
    //                               .ConfigureServices((hostBuilder, services) =>
    //                                                  {
    //                                                      services.AddHostedService<LabHostedService>();
    //                                                      Console.WriteLine($"注入 {nameof(LabHostedService)}");
    //                                                  });
    //         var host = hostBuilder.Build();
    //         host.RunAsync();
    //         Console.WriteLine($"{nameof(LabHostedService)} 應用程式已啟動");
    //         Console.ReadLine();
    //     }
    // }

    internal class Program
    {
        private static Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var task = host.RunAsync();
            host.WaitForShutdownAsync();
            Console.WriteLine($"{nameof(LabHostedService)} 應用程式已啟動");

            return task;
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                       .ConfigureServices((hostBuilder, services) =>
                                          {
                                              services.AddHostedService<LabHostedService>();
                                              services.AddHostedService<LabBackgroundService>();
                                              Console.WriteLine("注入HostService");
                                          })
                ;
        }
    }
}