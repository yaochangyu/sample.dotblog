using App.Metrics;
using App.Metrics.Core.Options;
using App.Metrics.Tagging;

namespace ConsoleApp.NET48
{
    public static class AppMetricsRegistery
    {
        public static class ProcessMetrics
        {
            private static readonly MetricTags CustomTags  = new MetricTags("tag-key", "tag-value");
            private static readonly string     ContextName = "Process";

            public static GaugeOptions SystemNonPagedMemoryGauge = new GaugeOptions
            {
                Context         = ContextName,
                Name            = "System Non-Paged Memory",
                MeasurementUnit = Unit.Bytes,
                Tags            = CustomTags
            };

            public static GaugeOptions ProcessVirtualMemorySizeGauge = new GaugeOptions
            {
                Context         = ContextName,
                Name            = "Process Virtual Memory Size",
                MeasurementUnit = Unit.Bytes,
                Tags            = CustomTags
            };
        }

        public static class DatabaseMetrics
        {
            private static readonly string ContextName = "Database";

            public static TimerOptions SearchUsersSqlTimer = new TimerOptions
            {
                Context         = ContextName,
                Name            = "Search Users",
                MeasurementUnit = Unit.Calls
            };
        }
    }
}