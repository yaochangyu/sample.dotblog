using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Lab.RateLimitQueueAPI.Middleware;
using Lab.RateLimitQueueAPI.Services;

// 建立 Web 應用程式產生器。
var builder = WebApplication.CreateBuilder(args);

// 將服務新增至容器中。
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Rate Limit Queue API", 
        Version = "v1",
        Description = "一個展示三種不同速率限制佇列機制的 API"
    });
    
    // 包含 XML 註解
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// 新增 CORS 服務
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 註冊佇列服務
builder.Services.AddSingleton<IQueueHandler, FifoQueueHandler>(); // 預設為 FIFO
builder.Services.AddSingleton<FifoQueueHandler>();
builder.Services.AddSingleton<PriorityQueueHandler>();
builder.Services.AddSingleton<AdaptiveDelayHandler>();

// 註冊背景服務
builder.Services.AddHostedService<QueueProcessorService>();

// 新增速率限制服務
builder.Services.AddRateLimiter(options =>
{
    // FIFO 策略 - 具有佇列的固定視窗
    options.AddFixedWindowLimiter("FifoPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 2; // 允許 2 個請求
        limiterOptions.Window = TimeSpan.FromSeconds(10); // 每 10 秒
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0; // 我們在中介軟體中處理佇列
    });

    // 優先權策略 - 具有佇列的固定視窗
    options.AddFixedWindowLimiter("PriorityPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 2; // 允許 2 個請求
        limiterOptions.Window = TimeSpan.FromSeconds(10); // 每 10 秒
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0; // 我們在中介軟體中處理佇列
    });

    // 適應性策略 - 用於動態速率限制的權杖桶
    options.AddTokenBucketLimiter("AdaptivePolicy", limiterOptions =>
    {
        limiterOptions.TokenLimit = 5; // 桶容量
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0; // 適應性策略沒有佇列
        limiterOptions.ReplenishmentPeriod = TimeSpan.FromSeconds(2); // 每 2 秒補充一次
        limiterOptions.TokensPerPeriod = 1; // 每個期間新增 1 個權杖
        limiterOptions.AutoReplenishment = true;
    });

    // 全域後備策略
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    // 當請求被拒絕時的處理邏輯
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        
        // 對於適應性策略，計算 retry-after
        if (context.HttpContext.GetEndpoint()?.Metadata
            .GetMetadata<Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute>()?.PolicyName == "AdaptivePolicy")
        {
            var adaptiveService = context.HttpContext.RequestServices.GetRequiredService<AdaptiveDelayHandler>();
            var clientId = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var currentLoad = adaptiveService.GetCurrentSystemLoad();
            var retryAfter = adaptiveService.CalculateRetryAfter(clientId, currentLoad);
            
            context.HttpContext.Response.Headers["Retry-After"] = retryAfter.ToString();
            
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                Error = "超過速率限制",
                Message = $"請求過多。請在 {retryAfter} 秒後重試。",
                RetryAfterSeconds = retryAfter,
                CurrentLoad = Math.Round(currentLoad, 2),
                Timestamp = DateTime.UtcNow
            }, cancellationToken: token);
        }
    };
});

// 建立 Web 應用程式。
var app = builder.Build();

// 設定 HTTP 請求管線。
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Rate Limit Queue API v1");
        c.RoutePrefix = string.Empty; // 在根目錄提供 Swagger UI
    });
}

app.UseCors();

// 新增自訂佇列中介軟體 (必須在 UseRateLimiter 之前以攔截 429s)
app.UseMiddleware<QueueMiddleware>();

// 新增速率限制中介軟體
app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

// 新增一個簡單的健康檢查端點
app.MapGet("/health", () => new { 
    Status = "Healthy", 
    Timestamp = DateTime.UtcNow,
    Version = "1.0.0"
});

// 執行應用程式。
app.Run("http://0.0.0.0:5000");

