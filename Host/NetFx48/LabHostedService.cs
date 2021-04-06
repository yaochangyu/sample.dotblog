using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NetFx48
{
    public class LabHostedService : IHostedService
    {
        private readonly ILogger _logger;

        public LabHostedService(ILogger<LabHostedService> logger,
                                IHostApplicationLifetime  lifetime)
        {
            this._logger = logger;

            lifetime.ApplicationStarted.Register(this.OnStarted);
            lifetime.ApplicationStopping.Register(this.OnStopping);
            lifetime.ApplicationStopped.Register(this.OnStopped);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation("1. StartAsync has been called.");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation("4. StopAsync has been called.");

            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            this._logger.LogInformation("2. OnStarted has been called.");
        }

        private void OnStopped()
        {
            this._logger.LogInformation("5. OnStopped has been called.");
        }

        private void OnStopping()
        {
            this._logger.LogInformation("3. OnStopping has been called.");
        }
    }
}