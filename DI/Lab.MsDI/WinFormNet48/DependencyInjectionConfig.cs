using System;
using Microsoft.Extensions.DependencyInjection;

namespace WinFormNet48
{
    internal class DependencyInjectionConfig
    {
        public static IServiceProvider ServiceProvider { get; set; }

        public static IServiceProvider Register()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
            return ServiceProvider;
        }

        /// <summary>
        ///     使用 MS DI 註冊
        /// </summary>
        private static IServiceCollection ConfigureServices(IServiceCollection services)
        {
            return services.AddSingleton<Form1>()
                           .AddTransient<Worker>()
                           .AddTransient<ITransientMessager, MultiMessager>()
                           .AddSingleton<ISingleMessager, MultiMessager>()
                           .AddScoped<IScopeMessager, MultiMessager>()

                //.AddTransient(provider =>
                //              {
                //                  var operation = provider.GetRequiredService<LogMessager>();
                //                  return new Worker(operation);
                //              })
                //.AddTransient(provider =>
                //              {
                //                  var operation = provider.GetRequiredService<LogMessager>();
                //                  return new Workflow(operation);
                //              })

                //.AddTransient<IOperation,CarOperation>()
                //.AddTransient<IOperation,HourseOperation>()
                //.AddLogging(loggingBuilder =>
                //            {
                //                // configure Logging with NLog
                //                loggingBuilder.ClearProviders();
                //                loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                //                loggingBuilder.AddNLog(config);
                //            })
                ;

            ;
        }

        //private static IConfiguration CreateConfig()
        //{
        //    var config = new ConfigurationBuilder()
        //                 .SetBasePath(System.IO.Directory
        //                                    .GetCurrentDirectory()) //From NuGet Package Microsoft.Extensions.Configuration.Json
        //                 .AddJsonFile("appsettings.json", true, true)
        //                 .Build();
        //    return config;
        //}
    }
}