using System;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace WinFormViaDiContainerNet48
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            var logger = LogManager.GetCurrentClassLogger();

            var config   = CreateConfig();
            var provider = CreateServiceProvider(config);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
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

        private static IServiceProvider CreateServiceProvider(IConfiguration config)
        {
            var serviceCollection = new ServiceCollection();
            return serviceCollection.AddTransient<Form1>() // Runner is the custom class
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