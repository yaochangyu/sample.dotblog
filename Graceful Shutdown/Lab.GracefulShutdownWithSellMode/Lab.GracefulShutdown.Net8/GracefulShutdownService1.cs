﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lab.GracefulShutdown.Net8;

class GracefulShutdownService1 : BackgroundService
{
    private readonly ILogger<GracefulShutdownService1> _logger;

    public GracefulShutdownService1(ILogger<GracefulShutdownService1> logger)
    {
        this._logger = logger;
    }

    private int _count = 1;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        this._logger.LogInformation($"{DateTime.Now} 服務已啟動!");
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            this._logger.LogInformation($"{DateTime.Now} ，執行次數：{_count++}");
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        this._logger.LogInformation($"{DateTime.Now} 完成!");
        return base.StopAsync(cancellationToken);
    }
}