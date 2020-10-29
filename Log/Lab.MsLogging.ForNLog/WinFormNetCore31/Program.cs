using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace WinFormNetCore31
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var config = CreateConfig();
            using (var serviceProvider = CreateServiceProvider(config))
            {
                var form = serviceProvider.GetService(typeof(Form1)) as Form;
                Application.Run(form);
            }
        }
        private static IConfiguration CreateConfig()
        {
            var config = new ConfigurationBuilder()
                         .SetBasePath(System.IO.Directory
                                            .GetCurrentDirectory()) //From NuGet Package Microsoft.Extensions.Configuration.Json
                         .AddJsonFile("appsettings.json", true, true)
                         .Build();
            return config;
        }

        private static ServiceProvider CreateServiceProvider(IConfiguration config)
        {
            var serviceCollection = new ServiceCollection();
            return serviceCollection.AddTransient<Form1>() // Runner is the custom class
                                    .AddTransient<Runner>()
                                    .AddLogging(loggingBuilder =>
                                                {
                                                    // configure Logging with NLog
                                                    loggingBuilder.ClearProviders();
                                                    loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                                                    loggingBuilder.AddNLog(config);
                                                })
                                    .BuildServiceProvider();
        }
    }
}
