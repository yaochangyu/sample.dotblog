using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace WinFormNet48
{
    internal static class Program
    {
        public static ServiceProvider ServiceProvider { get; set; }

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var serviceProvider = CreateServiceProvider())
            {
                ServiceProvider = serviceProvider;
                var form = serviceProvider.GetService(typeof(Form1)) as Form;
                Application.Run(form);
            }
        }

        private static ServiceProvider CreateServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            return serviceCollection.AddSingleton<Form1>()
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
                                    .BuildServiceProvider();
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