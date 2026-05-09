using Microsoft.Extensions.Caching.Hybrid;

namespace Lab.HybridCache;

public class TtlJitterCacheService(Microsoft.Extensions.Caching.Hybrid.HybridCache cache)
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public async Task<(WeatherForecast[] Data, string Source, TimeSpan L2Ttl)> GetAsync(
        string key, CancellationToken ct = default)
    {
        TimeSpan jitter = TimeSpan.FromSeconds(Random.Shared.Next(0, 120));
        TimeSpan l2Ttl = TimeSpan.FromMinutes(10) + jitter;
        TimeSpan l1Ttl = TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(Random.Shared.Next(0, 30));

        var options = new HybridCacheEntryOptions
        {
            Expiration = l2Ttl,
            LocalCacheExpiration = l1Ttl
        };

        string source = "L1/L2";
        var data = await cache.GetOrCreateAsync(
            key,
            async token =>
            {
                source = "DB";
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [TTL Jitter] 從 DB 載入資料，key={key}，L2 TTL={l2Ttl.TotalSeconds:F0}s");
                await Task.Delay(5, token);
                return Enumerable.Range(1, 5).Select(i => new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                }).ToArray();
            },
            options,
            cancellationToken: ct);

        return (data!, source, l2Ttl);
    }
}
