using Microsoft.Extensions.Caching.Hybrid;

namespace Lab.HybridCache.Avalanche;

public class LayeredTtlCacheService(Microsoft.Extensions.Caching.Hybrid.HybridCache cache)
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    private static readonly HybridCacheEntryOptions Options = new()
    {
        Expiration = TimeSpan.FromMinutes(30),        // L2 TTL
        LocalCacheExpiration = TimeSpan.FromMinutes(3) // L1 TTL < L2 TTL
    };

    public async Task<(WeatherForecast[] Data, string Source)> GetAsync(
        string key, CancellationToken ct = default)
    {
        string source = "L1/L2";
        var data = await cache.GetOrCreateAsync(
            key,
            async token =>
            {
                source = "DB";
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Layered TTL] 從 DB 載入資料，key={key}，L1=3m，L2=30m");
                await Task.Delay(5, token);
                return Enumerable.Range(1, 5).Select(i => new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                }).ToArray();
            },
            Options,
            cancellationToken: ct);

        return (data!, source);
    }
}
