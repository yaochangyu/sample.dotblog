using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace WinFormNet48
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var serviceProvider = CreateServiceProvider())
            {
                var form = serviceProvider.GetService(typeof(Form1)) as Form;
                Application.Run(form);
            }
        }
        private static ServiceProvider CreateServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            return serviceCollection.AddTransient<Form1>()
                                    .AddTransient<Worker>()
                                    .AddTransient<LogMessager>()
                                    .AddTransient<MachineMessager>()
                                    .AddTransient<Worker>(provider =>
                                                             {
                                                                 var operation = provider.GetRequiredService<MachineMessager>();
                                                                 return new Worker(operation);
                                                             })
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
