using Microsoft.Extensions.Caching.Hybrid;
using Lab.HybridCache;

namespace Lab.HybridCache;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAuthorization();

        // 設定 Redis 作為 L2 快取
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetConnectionString("Redis");
        });

        // 註冊 HybridCache 服務（L1 + L2 快取）
        builder.Services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromSeconds(30),
                LocalCacheExpiration = TimeSpan.FromSeconds(10)
            };
        });

        builder.Services.AddSingleton<TtlJitterCacheService>();
        builder.Services.AddSingleton<LayeredTtlCacheService>();
        builder.Services.AddSingleton<CircuitBreakerCacheService>();
        builder.Services.AddHostedService<CacheWarmupService>();

        builder.Services.AddOpenApi();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();

        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        // 原始端點
        app.MapGet("/weatherforecast", (HttpContext httpContext) =>
            {
                var forecast = Enumerable.Range(1, 5).Select(index =>
                        new WeatherForecast
                        {
                            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                            TemperatureC = Random.Shared.Next(-20, 55),
                            Summary = summaries[Random.Shared.Next(summaries.Length)]
                        })
                    .ToArray();
                return forecast;
            })
            .WithName("GetWeatherForecast");

        // HybridCache 基本用法
        app.MapGet("/weatherforecast/hybrid-cache",
                async (Microsoft.Extensions.Caching.Hybrid.HybridCache hybridCache) =>
                {
                    var forecast = await hybridCache.GetOrCreateAsync(
                        key: "weather-forecast",
                        factory: async cancellationToken =>
                        {
                            await Task.Delay(TimeSpan.FromMicroseconds(2), cancellationToken);
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 從資料來源生成天氣預報資料");
                            return Enumerable.Range(1, 5).Select(index =>
                                    new WeatherForecast
                                    {
                                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                                        TemperatureC = Random.Shared.Next(-20, 55),
                                        Summary = summaries[Random.Shared.Next(summaries.Length)]
                                    })
                                .ToArray();
                        });

                    return Results.Ok(new
                    {
                        Data = forecast,
                        Source = "L1/L2",
                        CachedAt = DateTime.Now,
                        Message = "資料來自 HybridCache (L1: 記憶體 + L2: Redis)"
                    });
                })
            .WithName("GetWeatherForecastWithHybridCache");

        // 策略一：TTL Jitter
        app.MapGet("/weatherforecast/ttl-jitter",
                async (TtlJitterCacheService svc, CancellationToken ct) =>
                {
                    var (data, source, l2Ttl) = await svc.GetAsync("weather-forecast-jitter", ct);
                    return Results.Ok(new
                    {
                        Data = data,
                        Source = source,
                        L2TtlSeconds = (int)l2Ttl.TotalSeconds,
                        Message = "策略一：TTL Jitter — 各 key 的失效時間隨機分散，避免集體失效"
                    });
                })
            .WithName("GetWeatherForecastTtlJitter");

        // 策略二：分層 TTL
        app.MapGet("/weatherforecast/layered-ttl",
                async (LayeredTtlCacheService svc, CancellationToken ct) =>
                {
                    var (data, source) = await svc.GetAsync("weather-forecast-layered", ct);
                    return Results.Ok(new
                    {
                        Data = data,
                        Source = source,
                        L1TtlMinutes = 3,
                        L2TtlMinutes = 30,
                        Message = "策略二：分層 TTL — L1=3m，L2=30m；L1 過期後仍從 L2 回傳，DB 幾乎零壓力"
                    });
                })
            .WithName("GetWeatherForecastLayeredTtl");

        // 策略三：Circuit Breaker
        app.MapGet("/weatherforecast/circuit-breaker",
                async (CircuitBreakerCacheService svc, CancellationToken ct) =>
                {
                    var (data, source, isFallback) = await svc.GetAsync("weather-forecast-cb", ct);
                    return Results.Ok(new
                    {
                        Data = data,
                        Source = source,
                        IsFallback = isFallback,
                        Message = isFallback
                            ? "Circuit Breaker 熔斷中，回傳降級資料"
                            : "策略三：Circuit Breaker — Redis 失敗率 > 50% 時熔斷 30 秒，回傳降級資料"
                    });
                })
            .WithName("GetWeatherForecastCircuitBreaker");

        app.Run();
    }
}
