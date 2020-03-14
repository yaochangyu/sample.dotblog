using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp.NET48
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var cancelSource = new CancellationTokenSource();

            var service = new ServiceCollection();
            service.ConfigureMetrics();
            service.ConfigureServices();
            var provider = service.BuildServiceProvider();
            provider.ScheduleMetricReporting(cancelSource);

            System.Console.WriteLine("Running Reports...");
            System.Console.ReadKey();
        }
    }
}