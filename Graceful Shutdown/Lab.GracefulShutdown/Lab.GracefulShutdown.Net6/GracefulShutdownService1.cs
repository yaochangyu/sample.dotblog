using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lab.GracefulShutdown.Net6;

class GracefulShutdownService1 : BackgroundService
{
    private readonly ILogger<GracefulShutdownService1> _logger;

    public GracefulShutdownService1(ILogger<GracefulShutdownService1> logger)
    {
        this._logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this._logger.LogInformation($"{DateTime.Now} 服務已啟動!");
        while (!stoppingToken.IsCancellationRequested)
        {
            this._logger.LogInformation($"{DateTime.Now} 1.服務運行中...");
            this._logger.LogInformation($"1.IsCancel={stoppingToken.IsCancellationRequested}");
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            this._logger.LogInformation($"2.IsCancel={stoppingToken.IsCancellationRequested}");
            this._logger.LogInformation($"{DateTime.Now} 2.服務運行中...");
        }
        this._logger.LogInformation($"{DateTime.Now} 服務已完美的停止(Graceful Shutdown)");
    }
}