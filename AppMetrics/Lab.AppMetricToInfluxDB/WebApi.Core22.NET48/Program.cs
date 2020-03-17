using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.AspNetCore;
using App.Metrics.Filtering;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebApi.Core22.NET48
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var filter = new MetricsFilter();

            return WebHost.CreateDefaultBuilder(args)
                          .UseStartup<Startup>()
                          .ConfigureMetricsWithDefaults(builder =>
                                                        {
                                                            builder.Filter.With(filter);
                                                            builder.Report.ToInfluxDb(AppSetting.InfluxDB.Url,
                                                                                      AppSetting.InfluxDB.DatabaseName,
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
                          .UseMetricsEndpoints();
        }
    }
}
