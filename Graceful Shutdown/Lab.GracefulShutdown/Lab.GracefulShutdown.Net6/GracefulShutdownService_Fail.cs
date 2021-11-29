using Microsoft.Extensions.Hosting;

namespace Lab.GracefulShutdown.Net6;

internal class GracefulShutdownService_Fail : IHostedService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private bool _stop;

    public GracefulShutdownService_Fail(IHostApplicationLifetime appLifetime)
    {
        this._appLifetime = appLifetime;
    }

    public async Task StartAsync(CancellationToken cancel)
    {
        Console.WriteLine($"{DateTime.Now} 服務啟動中...");
        await this.ExecuteAsync(cancel);
    }

    public Task StopAsync(CancellationToken cancel)
    {
        this._stop = true;
        Console.WriteLine("服務關閉");
        return Task.CompletedTask;
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