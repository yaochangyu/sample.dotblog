using Microsoft.Extensions.Caching.Hybrid;

namespace Lab.HybridCache;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();

        // 註冊 HybridCache 服務（L1 + L2 快取）
        builder.Services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromSeconds(30), //L2 快取時間（Redis）
                LocalCacheExpiration = TimeSpan.FromSeconds(10) //L1 快取時間（記憶體）
            };
        });

        // 設定 Redis 作為 L2 快取
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            var redisConnection = builder.Configuration.GetConnectionString("Redis");

            options.Configuration = redisConnection;
        });

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        // 原始的天氣預報 API
        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

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


        app.MapGet("/weatherforecast/hybrid-cache", 
                async (Microsoft.Extensions.Caching.Hybrid.HybridCache hybridCache) =>
            {
                // 使用 HybridCache 快取天氣預報資料
                var forecast = await hybridCache.GetOrCreateAsync(
                    key: "weather-forecast",
                    factory: async cancellationToken =>
                    {
                        // 模擬從資料來源獲取資料（這裡會有延遲以便觀察快取效果）
                        await Task.Delay(TimeSpan.FromMicroseconds(2), cancellationToken); // 模擬 2 秒的資料庫查詢

                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 從資料來源生成天氣預報資料");

                        return Enumerable.Range(1, 5).Select(index =>
                                new WeatherForecast
                                {
                                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                                    TemperatureC = Random.Shared.Next(-20, 55),
                                    Summary = summaries[Random.Shared.Next(summaries.Length)]
                                })
                            .ToArray();
                    }
                    );

                return Results.Ok(new
                {
                    Data = forecast,
                    CachedAt = DateTime.Now,
                    Message = "資料來自 HybridCache (L1: 記憶體 + L2: Redis)"
                });
            })
            .WithName("GetWeatherForecastWithHybridCache")
            .WithSummary("使用 HybridCache 的天氣預報")
            .WithDescription("展示 HybridCache L1（記憶體）+ L2（Redis）雙層快取功能");

        app.Run();
    }
}