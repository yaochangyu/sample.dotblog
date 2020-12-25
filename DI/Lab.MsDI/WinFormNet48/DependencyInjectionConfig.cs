using Microsoft.Extensions.DependencyInjection;

namespace WinFormNet48
{
    public class DependencyInjectionConfig
    {
        public static IServiceCollection ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection();
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