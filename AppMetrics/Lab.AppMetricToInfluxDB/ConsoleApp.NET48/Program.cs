using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Health;
using App.Metrics.Health.Builder;
using App.Metrics.Scheduling;

namespace ConsoleApp.NET48
{
    internal class Program
    {
        public static async Task Main()
        {
            long threshold      = 1;
            var  metricsBuilder = new MetricsBuilder();
            var  healthBuilder  = new HealthBuilder();

            var metrics = metricsBuilder.Report.ToInfluxDb(AppSetting.InfluxDB.Url,
                                                           AppSetting.InfluxDB.DatabaseName,
                                                           TimeSpan.FromSeconds(5))
                                        .Report.ToConsole(TimeSpan.FromSeconds(5))
                                        .Build()
                ;
            var health = healthBuilder.Configuration
                                      .Configure(p =>
                                                 {
                                                     p.Enabled          = true;
                                                     p.ReportingEnabled = true;
                                                 })
                                      .Report
                                      .ToMetrics(metrics)
                                      .HealthChecks.AddProcessPrivateMemorySizeCheck("Private Memory Size", threshold)
                                      .HealthChecks.AddProcessVirtualMemorySizeCheck("Virtual Memory Size", threshold)
                                      .HealthChecks.AddProcessPhysicalMemoryCheck("Working Set", threshold)
                                      .HealthChecks.AddPingCheck("google ping", "google.com", TimeSpan.FromSeconds(10))
                                      .Build();

            var counter = new CounterOptions {Name = "my_counter"};
            metrics.Measure.Counter.Increment(counter);

            var scheduler =
                new AppMetricsTaskScheduler(TimeSpan.FromSeconds(5),
                                            async () =>
                                            {
                                                await Task.WhenAll(metrics.ReportRunner.RunAllAsync());
                                                var healthStatus =
                                                    await health.HealthCheckRunner.ReadAsync();

                                                using (var stream = new MemoryStream())
                                                {
                                                    await health.DefaultOutputHealthFormatter
                                                                .WriteAsync(stream, healthStatus);
                                                    var result = Encoding.UTF8.GetString(stream.ToArray());
                                                    Console.WriteLine(result);
                                                }

                                                foreach (var reporter in health.Reporters)
                                                {
                                                    await reporter.ReportAsync(health.Options,
                                                                               healthStatus);
                                                }
                                            });
            scheduler.Start();

            Console.ReadKey();
        }
    }
}