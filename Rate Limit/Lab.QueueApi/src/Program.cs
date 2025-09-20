using Lab.QueueApi.RateLimit;
using Lab.QueueApi.Services;

// 建立 WebApplication 建構器。
var builder = WebApplication.CreateBuilder(args);

// 將控制器服務加入到容器中。
builder.Services.AddControllers();

// 學習更多關於設定 Swagger/OpenAPI 的資訊，請參考 https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // 設定 Swagger 文件
    c.SwaggerDoc("v1", new()
    {
        Title = "Queued Web API",
        Version = "v1",
        Description = "一個具有速率限制和佇列機制的 Web API"
    });

    // 包含 XML 註解以提供更詳細的 API 文件
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// 多少時間內
var TimeWindow = TimeSpan.FromSeconds(10);

// 處理多少請求
var RequestCount = 2;

// 排隊最大請求輛
var MaxRequestCapacity = 100;

// 過期請求清理設定
var MaxRequestAge = TimeSpan.FromMinutes(1);
var CleanupInterval = TimeSpan.FromSeconds(5);

// 佇列處理服務設定
var ProcessingDelay = TimeSpan.FromSeconds(1);     // 處理請求的模擬延遲
var EmptyQueueDelay = TimeSpan.FromMilliseconds(100);  // 佇列空時的等待間隔
var ErrorRetryDelay = TimeSpan.FromSeconds(1);     // 錯誤重試延遲

// 註冊自訂服務
// 註冊 SlidingWindowRateLimiter 作為 IRateLimiter 的單例服務
builder.Services.AddSingleton<IRateLimiter>(provider =>
    new SlidingWindowRateLimiter(maxRequests: RequestCount, timeWindow: TimeWindow));

// 註冊 ChannelRequestQueueProvider 作為 IRequestQueueProvider 的單例服務
builder.Services.AddSingleton<ICommandQueueProvider>(provider =>
    new ChannelCommandQueueProvider(capacity: MaxRequestCapacity));

// 註冊 ChannelRequestQueueService 作為一個託管服務，使其在背景執行
builder.Services.AddHostedService<ChannelCommandQueueService>(provider =>
    new ChannelCommandQueueService(
        provider.GetRequiredService<ICommandQueueProvider>(),
        provider.GetRequiredService<IRateLimiter>(),
        provider.GetRequiredService<ILogger<ChannelCommandQueueService>>(),
        ProcessingDelay,
        EmptyQueueDelay,
        ErrorRetryDelay));

// 註冊過期請求清理服務作為背景服務
builder.Services.AddHostedService<ExpiredRequestCleanupService>(provider =>
    new ExpiredRequestCleanupService(
        provider.GetRequiredService<ICommandQueueProvider>(),
        provider.GetRequiredService<ILogger<ExpiredRequestCleanupService>>(),
        MaxRequestAge,
        CleanupInterval));

// 加入 CORS 支援
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin() // 允許任何來源
            .AllowAnyMethod() // 允許任何 HTTP 方法
            .AllowAnyHeader(); // 允許任何 HTTP 標頭
    });
});

// 加入記錄服務
builder.Services.AddLogging(logging =>
{
    logging.AddConsole(); // 將日誌輸出到主控台
    logging.AddDebug(); // 將日誌輸出到偵錯視窗
});

// 建構 WebApplication。
var app = builder.Build();

// 設定 HTTP 請求管線。
if (app.Environment.IsDevelopment())
{
    // 在開發環境中啟用 Swagger 和 Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Queued Web API v1");
        c.RoutePrefix = string.Empty; // 將 Swagger UI 設定在應用程式的根目錄
    });
}

// 使用 HTTPS 重新導向。
app.UseHttpsRedirection();

// 使用 CORS。
app.UseCors();

// 使用授權。
app.UseAuthorization();

// 對應控制器。
app.MapControllers();

// 加入一個簡單的健康檢查端點
app.MapGet("/", () => new
{
    Service = "Queued Web API",
    Version = "1.0.0",
    Status = "Running",
    Timestamp = DateTime.UtcNow,
    Documentation = "/swagger"
});

// 執行應用程式。
app.Run();