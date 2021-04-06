using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NetFx48
{
    public class AppHost : IHostedService
    {
        private readonly IHostApplicationLifetime appLifetime;
        private readonly ILogger<AppHost>         logger;

        public AppHost(ILogger<AppHost> logger, IHostApplicationLifetime appLifetime)
        {
            this.logger      = logger;
            this.appLifetime = appLifetime;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.logger.LogWarning("App running at: {time}", DateTimeOffset.Now);

            await Task.Yield();

            this.appLifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.logger.LogWarning("App stopped at: {time}", DateTimeOffset.Now);
            return Task.CompletedTask;
        }
    }
}