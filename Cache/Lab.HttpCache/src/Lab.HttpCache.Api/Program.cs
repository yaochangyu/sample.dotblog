var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// 加入 Controller 支援
builder.Services.AddControllers();

// 加入 Response Cache
builder.Services.AddResponseCaching();

// 註冊 Memory Cache
builder.Services.AddMemoryCache();
builder.Services.AddScoped<Lab.HttpCache.Api.Services.IMemoryCacheService, Lab.HttpCache.Api.Services.MemoryCacheService>();

// 註冊 Redis Cache
var redisConfiguration = builder.Configuration["Redis:Configuration"];
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConfiguration;
});
builder.Services.AddScoped<Lab.HttpCache.Api.Services.IRedisCacheService, Lab.HttpCache.Api.Services.RedisCacheService>();

// 註冊二級快取
builder.Services.AddScoped<Lab.HttpCache.Api.Services.ITwoLevelCacheService, Lab.HttpCache.Api.Services.TwoLevelCacheService>();

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
