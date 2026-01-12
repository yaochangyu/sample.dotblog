using System.Threading.RateLimiting;
using Lab.CSRF2.WebAPI.Providers;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ITokenProvider, TokenProvider>();
builder.Services.AddOpenApi();

// 新增速率限制
builder.Services.AddRateLimiter(options =>
{
    // API 端點速率限制: 10 秒內最多 10 次請求
    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromSeconds(10);
        limiterOptions.PermitLimit = 10;
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Token 生成速率限制: 1 分鐘內最多 5 個 Token
    options.AddFixedWindowLimiter("token", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 5;
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.RejectionStatusCode = 429; // Too Many Requests
});

// 修正 CORS 設定 - 限制允許的來源
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:5073",
                "https://localhost:5073",
                "http://localhost:7001",
                "https://localhost:7001"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-CSRF-Token")
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRateLimiter(); // 新增速率限制中介層
app.UseCors();
app.UseStaticFiles();
app.MapControllers();

app.Run();
