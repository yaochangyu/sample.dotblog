using Microsoft.Extensions.Caching.Hybrid;
using Polly;
using Polly.CircuitBreaker;

namespace Lab.HybridCache;

public class CircuitBreakerCacheService(Microsoft.Extensions.Caching.Hybrid.HybridCache cache)
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    private static readonly HybridCacheEntryOptions Options = new()
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };

    private readonly ResiliencePipeline _pipeline = new ResiliencePipelineBuilder()
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 3,
            SamplingDuration = TimeSpan.FromSeconds(10),
            BreakDuration = TimeSpan.FromSeconds(30),
            OnOpened = args =>
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Circuit Breaker] 熔斷觸發，暫停 30 秒");
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Circuit Breaker] 熔斷關閉，恢復正常");
                return ValueTask.CompletedTask;
            }
        })
        .Build();

    public async Task<(WeatherForecast[]? Data, string Source, bool IsFallback)> GetAsync(
        string key, CancellationToken ct = default)
    {
        try
        {
            WeatherForecast[]? result = null;
            string source = "L1/L2";

            await _pipeline.ExecuteAsync(async token =>
            {
                result = await cache.GetOrCreateAsync(
                    key,
                    async innerToken =>
                    {
                        source = "DB";
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Circuit Breaker] 從 DB 載入資料，key={key}");
                        await Task.Delay(5, innerToken);
                        return Enumerable.Range(1, 5).Select(i => new WeatherForecast
                        {
                            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
                            TemperatureC = Random.Shared.Next(-20, 55),
                            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                        }).ToArray();
                    },
                    Options,
                    cancellationToken: token);
            }, ct);

            return (result, source, false);
        }
        catch (BrokenCircuitException)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Circuit Breaker] 熔斷中，回傳降級資料");
            return ([], "Fallback", true);
        }
    }
}
