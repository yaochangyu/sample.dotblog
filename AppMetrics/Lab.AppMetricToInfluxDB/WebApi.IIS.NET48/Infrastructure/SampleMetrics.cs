using App.Metrics;
using App.Metrics.Core.Options;

namespace WebApi.IIS.NET48.Infrastructure
{
    public static class SampleMetrics
    {
        public static CounterOptions BasicCounter = new CounterOptions
        {
            Name             = "sample_counter",
            MeasurementUnit  = Unit.Calls,
            ResetOnReporting = true
        };
    }
}