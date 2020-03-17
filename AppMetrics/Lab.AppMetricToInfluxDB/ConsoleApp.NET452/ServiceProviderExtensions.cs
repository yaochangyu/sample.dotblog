using System;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;

namespace ConsoleApp.NET452
{
    public static class ServiceProviderExtensions
    {
        public static IServiceCollection ConfigureServices(this IServiceCollection services)
        {
            var env = PlatformServices.Default.Application;

            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole((l, s) => { return s == LogLevel.Trace; });
            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddLogging();

            return services;
        }

        public static void ScheduleMetricReporting(this IServiceProvider   provider,
                                                   CancellationTokenSource cancellationTokenSource)
        {
            var application = new Application(provider);
            var scheduler   = new DefaultTaskScheduler();

            Task.Factory
                .StartNew(() =>
                          {
                              scheduler.Interval(TimeSpan.FromMilliseconds(300),
                                                 TaskCreationOptions.None,
                                                 () =>
                                                 {
                                                     // Run Metrics

                                                 },
                                                 cancellationTokenSource.Token);
                          });

            Task.Factory
                .StartNew(() =>
                          {
                              application.Reporter.RunReports(application.Metrics,
                                                              cancellationTokenSource.Token);
                          });
        }
    }
}