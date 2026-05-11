namespace Lab.HybridCache.Avalanche;

public class CacheWarmupService(
    Microsoft.Extensions.Caching.Hybrid.HybridCache cache,
    ILogger<CacheWarmupService> logger) : IHostedService
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    private static readonly string[] HotKeys =
    [
        "weather-forecast-taipei",
        "weather-forecast-tokyo"
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var key in HotKeys)
            try
            {
                await cache.GetOrCreateAsync(
                    key,
                    async ct =>
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Warmup] 預熱 key={key}");
                        await Task.Delay(5, ct);
                        return Enumerable.Range(1, 5).Select(i => new WeatherForecast
                        {
                            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
                            TemperatureC = Random.Shared.Next(-20, 55),
                            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                        }).ToArray();
                    },
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[Warmup] 預熱 key={Key} 失敗，跳過", key);
            }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}