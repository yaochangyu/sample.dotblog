using AspNetCoreRateLimit;
using Lab.CSRF.WebApi.Middleware;
using Lab.CSRF.WebApi.Providers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Anti-Forgery 配置 (用於 CSRF 防護)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "XSRF-TOKEN";
    options.Cookie.HttpOnly = false;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    // Cookie.Expiration 已被棄用，Token 過期由 Anti-Forgery 內部管理
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
              .WithHeaders("Content-Type", "X-CSRF-TOKEN", "X-Nonce")
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
