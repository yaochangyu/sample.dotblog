using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleAppNetFx48
{
    public class LabHostedService : IHostedService
    {
        private readonly ILogger _logger;

        public LabHostedService(ILogger<LabHostedService> logger,
                                IHostApplicationLifetime  appLifetime,
                                IHostLifetime             hostLifetime,
                                IHostEnvironment          hostEnvironment)
        {
            this._logger = logger;
            appLifetime.ApplicationStarted.Register(this.OnStarted);
            appLifetime.ApplicationStopping.Register(this.OnStopping);
            appLifetime.ApplicationStopped.Register(this.OnStopped);
            this._logger.LogInformation($"主機環境："                                           +
                                        $"ApplicationName = {hostEnvironment.ApplicationName}\r\n" +
                                        $"EnvironmentName = {hostEnvironment.EnvironmentName}\r\n" +
                                        $"RootPath = {hostEnvironment.ContentRootPath}\r\n"        +
                                        $"Root File Provider = {hostEnvironment.ContentRootFileProvider}\r\n");
        }
     
        public Task StartAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation("1. 調用 Host.StartAsync ");
            return Task.CompletedTask;
        }
        private void OnStarted()
        {
            this._logger.LogInformation("2. 調用 OnStarted");
        }
        private void OnStopping()
        {
            this._logger.LogInformation("3. 調用 OnStopping");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation("4. 調用 Host.StopAsync");
            return Task.CompletedTask;
        }
     
        private void OnStopped()
        {
            this._logger.LogInformation("5. 調用 OnStopped");
        }
    }
}