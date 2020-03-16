using System;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics.Health;

namespace WebApiCore31
{
    public class SampleHealthCheck : HealthCheck
    {
        public SampleHealthCheck()
            : base("Sample Health Check") { }

        protected override ValueTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
        {
            if (DateTime.UtcNow.Second <= 20)
            {
                return new ValueTask<HealthCheckResult>(HealthCheckResult.Degraded());
            }

            if (DateTime.UtcNow.Second >= 40)
            {
                return new ValueTask<HealthCheckResult>(HealthCheckResult.Unhealthy());
            }

            return new ValueTask<HealthCheckResult>(HealthCheckResult.Healthy());
        }
    }
}