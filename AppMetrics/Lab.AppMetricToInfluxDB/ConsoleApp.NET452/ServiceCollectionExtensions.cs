using System;
using System.Reflection;
using App.Metrics;
using App.Metrics.Extensions.Reporting.Console;
using App.Metrics.Extensions.Reporting.InfluxDB;
using App.Metrics.Extensions.Reporting.InfluxDB.Client;
using App.Metrics.Filtering;
using App.Metrics.Reporting.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp.NET452
{
    public static class ServiceCollectionExtensions
    {
        private const string influxUrl      = "http://127.0.0.1:8086";
        private const string influxDatabase = "appmetricsnet452";

        public static IServiceCollection ConfigureMetrics(this IServiceCollection services)
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName();
            services.AddMetrics(p =>
                                {
                                    p.DefaultContextLabel = "Demo";
                                    p.ReportingEnabled    = true;
                                    p.GlobalTags.Add("env", "stage");
                                    p.MetricsEnabled = true;
                                }, assemblyName)
                    .AddHealthChecks(factory =>
                                     {
                                         factory.RegisterProcessPrivateMemorySizeHealthCheck("Private Memory Size",
                                                                                             200);
                                         factory.RegisterProcessVirtualMemorySizeHealthCheck("Virtual Memory Size",
                                                                                             200);
                                         factory.RegisterProcessPhysicalMemoryHealthCheck("Working Set", 200);
                                     })
                    .AddReporting(factory =>
                                  {
                                      factory.AddConsole(new ConsoleReporterSettings
                                      {
                                          ReportInterval = TimeSpan.FromSeconds(5)
                                      });

                                      var influxFilter = new DefaultMetricsFilter()
                                                         .WhereMetricTaggedWithKeyValue(new TagKeyValueFilter
                                                                                            {{"reporter", "influxdb"}})
                                                         .WithHealthChecks(true)
                                                         .WithEnvironmentInfo(true);

                                      factory.AddInfluxDb(new InfluxDBReporterSettings
                                      {
                                          HttpPolicy = new HttpPolicy
                                          {
                                              FailuresBeforeBackoff = 3,
                                              BackoffPeriod         = TimeSpan.FromSeconds(10),
                                              Timeout               = TimeSpan.FromSeconds(3)
                                          },
                                          InfluxDbSettings = new InfluxDBSettings(influxDatabase, new Uri(influxUrl)),
                                          ReportInterval   = TimeSpan.FromSeconds(5)
                                      }, influxFilter);
                                  })
                    .AddMetricsMiddleware()
                ;

            return services;
        }
    }
}