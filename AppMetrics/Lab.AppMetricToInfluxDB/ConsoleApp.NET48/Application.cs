using System;
using System.Threading;
using App.Metrics;
using App.Metrics.Reporting.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp.NET48
{
    public class Application
    {
        public IMetrics Metrics { get; set; }

        public IReporter Reporter { get; set; }

        public CancellationToken Token { get; set; }

        public Application(IServiceProvider provider)
        {
            this.Metrics = provider.GetRequiredService<IMetrics>();

            var reporterFactory = provider.GetRequiredService<IReportFactory>();
            this.Reporter = reporterFactory.CreateReporter();

            this.Token = new CancellationToken();
        }
    }
}