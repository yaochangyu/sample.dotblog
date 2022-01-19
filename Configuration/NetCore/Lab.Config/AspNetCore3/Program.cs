using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AspNetCore3
{
    public class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                       .ConfigureWebHostDefaults(webBuilder =>
                       {
                           webBuilder.ConfigureAppConfiguration(p =>
                           {
                               p.AddJsonFile("appsettings.json", false, false);
                           });
                           webBuilder.UseStartup<Startup>();

                           // webBuilder.UseStartup<StartupInjectionOptions>();
                           // webBuilder.UseStartup<StartupInjectionOptionsSnapshot>();
                           // webBuilder.UseStartup<StartupInjectionOptionsMonitor>();
                           // webBuilder.UseStartup<StartupInjectionAppSetting>();
                       });
        }

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
    }
}