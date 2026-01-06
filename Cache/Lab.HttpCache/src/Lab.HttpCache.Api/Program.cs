var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// 加入 Controller 支援
builder.Services.AddControllers();

// 加入 Response Cache
builder.Services.AddResponseCaching();

// 註冊 HybridCache (.NET 9 新功能)
// HybridCache 自動整合 L1 (記憶體) 和 L2 (分散式) 快取
var redisConfiguration = builder.Configuration["Redis:Configuration"];
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConfiguration;
});

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };
});

// 註冊自訂快取服務
builder.Services.AddScoped<Lab.HttpCache.Api.Services.ICacheService, Lab.HttpCache.Api.Services.HybridCacheService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 使用 Response Cache 中介軟體
app.UseResponseCaching();

// 對應 Controllers
app.MapControllers();

app.Run();
