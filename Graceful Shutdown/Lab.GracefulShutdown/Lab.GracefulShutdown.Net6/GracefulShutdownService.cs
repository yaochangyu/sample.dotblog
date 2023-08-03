using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lab.GracefulShutdown.Net6;

internal class GracefulShutdownService : IHostedService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private Task _backgroundTask;
    private bool _stop;
    private ILogger<GracefulShutdownService> _logger;

    public GracefulShutdownService(IHostApplicationLifetime appLifetime,
        ILogger<GracefulShutdownService> logger)
    {
        this._appLifetime = appLifetime;
        this._logger = logger;
    }

    public Task StartAsync(CancellationToken cancel)
    {
        this._logger.LogInformation($"{DateTime.Now} 服務啟動中...");

        this._backgroundTask = Task.Run(async () => { await this.ExecuteAsync(cancel); }, cancel);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancel)
    {
        this._logger.LogInformation($"{DateTime.Now} 服務停止中...");

        this._stop = true;
        await this._backgroundTask;
        
        this._logger.LogInformation($"{DateTime.Now} 服務已停止");
    }

    private async Task ExecuteAsync(CancellationToken cancel)
    {
        this._logger.LogInformation($"{DateTime.Now} 服務已啟動!");

        while (!this._stop)
        {
            this._logger.LogInformation($"{DateTime.Now} 1.服務運行中...");
            this._logger.LogInformation($"1.IsCancel={cancel.IsCancellationRequested}");
            await Task.Delay(TimeSpan.FromSeconds(30), cancel);
            this._logger.LogInformation($"2.IsCancel={cancel.IsCancellationRequested}");
            this._logger.LogInformation($"{DateTime.Now} 2.服務運行中...");
        }

        this._logger.LogInformation($"{DateTime.Now} 服務已完美的停止(Graceful Shutdown)");
    }
}