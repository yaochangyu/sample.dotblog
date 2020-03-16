using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.AspNetCore;
using App.Metrics.AspNetCore.Health;
using App.Metrics.Filtering;
using App.Metrics.Health;
using App.Metrics.Health.Builder;
using App.Metrics.Scheduling;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WebApiCore31
{
    public class Program
    {
        public const string InfluxDbUrl  = "http://127.0.0.1:8086";
        public const string InfluxDbName = "AppMetricsAspCore31";

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var thresholdBytes = 200;
            var filter         = new MetricsFilter();

            return Host.CreateDefaultBuilder(args)
                       .ConfigureWebHostDefaults(webBuilder =>
                                                 {
                                                     //webBuilder.ConfigureHealthWithDefaults((context, healthBuilder) =>
                                                     //                                       {
                                                     //                                       });
                                                     webBuilder.UseHealthEndpoints();
                                                     webBuilder.UseHealth();
                                                     webBuilder.UseStartup<Startup>();

                                                 })
                       .ConfigureMetricsWithDefaults(builder =>
                                                     {
                                                         builder.Filter.With(filter);
                                                         builder.Report.ToInfluxDb(InfluxDbUrl, InfluxDbName,
                                                                                   TimeSpan.FromSeconds(5));
                                                         builder.Report.ToConsole(TimeSpan.FromSeconds(5));
                                                         builder.Configuration
                                                                .Configure(p =>
                                                                           {
                                                                               p.Enabled          = true;
                                                                               p.ReportingEnabled = true;
                                                                           })
                                                             ;
                                                     })
                       .UseMetrics()
                       .UseMetricsWebTracking()
                ;
        }


        public static void Main(string[] args)
        {
            var build = CreateHostBuilder(args).Build();
            build.Run();
        }
    }
}