using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebApiNetCore31
{
    public class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                       .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                       .ConfigureLogging((context, builder) =>
                                         {
                                             builder
                                                 .AddFilter("Microsoft",        LogLevel.Warning)
                                                 .AddFilter("System",           LogLevel.Warning)
                                                 .AddFilter("WindowsFormsApp1", LogLevel.Debug)
                                                 .AddConfiguration(context.Configuration.GetSection("Logging"))
                                                 .AddConsole()
                                                 ;
                                         });
        }

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
    }
}