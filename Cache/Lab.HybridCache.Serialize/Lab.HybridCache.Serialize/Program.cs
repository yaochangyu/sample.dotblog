using Lab.HybridCache.Serialize.Models;
using Lab.HybridCache.Serialize.Serializers;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";

// BenchmarkController 用：直接操作 Redis（allowAdmin 供 INFO、MEMORY USAGE 等指令使用）
var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
redisOptions.AllowAdmin = true;
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisOptions));

// HybridCache 用：Redis 作為 L2 分散式快取
builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = redisConnectionString);

// 註冊 HybridCache，並指定 ProductModel 的序列化器
// 切換序列化器只需更換 AddSerializer 的泛型型別參數：
//   MessagePack → AddSerializer<ProductModel, MessagePackHybridCacheSerializer<ProductModel>>()
//   MemoryPack  → AddSerializer<ProductModel, MemoryPackHybridCacheSerializer<ProductModel>>()
builder.Services.AddHybridCache()
    .AddSerializer<ProductModel, MessagePackHybridCacheSerializer<ProductModel>>();
// 若要改用 MemoryPack，替換上方一行為：
// .AddSerializer<ProductModel, MemoryPackHybridCacheSerializer<ProductModel>>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
