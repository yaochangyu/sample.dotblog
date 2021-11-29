using Microsoft.Extensions.Hosting;

namespace Lab.GracefulShutdown.Net6;

internal class GracefulShutdownService : IHostedService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private Task _backgroundTask;
    private bool _stop;

    public GracefulShutdownService(IHostApplicationLifetime appLifetime)
    {
        this._appLifetime = appLifetime;
    }

    public Task StartAsync(CancellationToken cancel)
    {
        Console.WriteLine($"{DateTime.Now} 服務啟動中...");

        this._backgroundTask = Task.Run(async () => { await this.ExecuteAsync(cancel); }, cancel);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancel)
    {
        Console.WriteLine($"{DateTime.Now} 服務停止中...");

        this._stop = true;
        await this._backgroundTask;

        Console.WriteLine($"{DateTime.Now} 服務已停止");
    }

    private async Task ExecuteAsync(CancellationToken cancel)
    {
        Console.WriteLine($"{DateTime.Now} 服務已啟動!");

        while (!this._stop)
        {
            Console.WriteLine($"{DateTime.Now} 服務運行中...");
            await Task.Delay(TimeSpan.FromSeconds(1), cancel);
        }

        Console.WriteLine($"{DateTime.Now} 服務已完美的停止(Graceful Shutdown)");
    }
}