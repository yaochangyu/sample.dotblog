using AspNetCoreRateLimit;
using Lab.CSRF.WebApi.Middleware;
using Lab.CSRF.WebApi.Providers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllersWithViews();  // 改為 AddControllersWithViews 以支援 ValidateAntiForgeryToken

// Anti-Forgery 配置 (用於 CSRF 防護)
// 參考: https://blog.darkthread.net/blog/spa-minapi-xsrf/
builder.Services.AddAntiforgery(options =>
{
    // Header 名稱，前端會將 Token 放入此 Header
    options.HeaderName = "X-XSRF-TOKEN";
    
    // CookieToken 的配置 (.AspNetCore.Antiforgery.XXX)
    // 注意：不要設定 Cookie.Name，讓系統自動產生 .AspNetCore.Antiforgery.XXX
    // HttpOnly 預設為 true，這是 CookieToken 應有的安全設定
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    
    // 註：RequestToken (X-XSRF-TOKEN) 會在 Controller 中手動加入，設為 HttpOnly=false
});

// Rate Limiting 設定 (使用 IDistributedCache)
builder.Services.AddDistributedMemoryCache(); // IDistributedCache 實作
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, DistributedCacheIpPolicyStore>();
builder.Services.AddSingleton<IClientPolicyStore, DistributedCacheClientPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Token Nonce Provider (防止重放攻擊)
builder.Services.AddSingleton<ITokenNonceProvider, TokenNonceProvider>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("RestrictedCors", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173", 
                          "http://localhost:5073", "https://localhost:5073")
              .WithMethods("GET", "POST", "OPTIONS")
              .WithHeaders("Content-Type", "X-XSRF-TOKEN", "X-Nonce")  // 改為 X-XSRF-TOKEN
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseIpRateLimiting();
app.UseCors("RestrictedCors");
app.UseMiddleware<SecurityLoggingMiddleware>();
app.UseAntiforgery();
app.MapControllers();

app.Run();
