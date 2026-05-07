using AuthServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<AuthorizationCodeStore>();
builder.Services.AddSingleton<PkceService>();
builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddSingleton<AccessTokenStore>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                "https://localhost:7070",
                "https://127.0.0.1:7070",
                "http://localhost:5283",
                "http://127.0.0.1:5283")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] =
        "accelerometer=(), camera=(), geolocation=(), gyroscope=(), microphone=(), payment=(), usb=()";

    // emulator.html 由 client 端透過 meta tag 動態管理 CSP，server 不介入
    if (!context.Request.Path.Equals("/emulator.html", StringComparison.OrdinalIgnoreCase))
    {
        // [Lab] style-src 保留 'unsafe-inline' 是因為 emulator.html 使用 <style> 標籤，
        // 刻意避免實作 nonce 以簡化示範。正式環境應改用 nonce 消除 CSS injection 風險。
        // [Lab] script-src 使用 'self' 允許同源所有 .js 檔；
        // 正式環境應改用 hash（'sha256-...'）或 nonce 精確鎖定允許的腳本。
        // cdn.jsdelivr.net 供 flow.html 載入 Mermaid.js 使用
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' https://cdn.jsdelivr.net; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "connect-src 'self'; " +
            "font-src 'self'; " +
            "object-src 'none'; " +
            "base-uri 'none'; " +
            "frame-ancestors 'none'; " +
            "form-action 'self'";
    }

    await next();
});

app.UseHttpsRedirection();
app.UseCors();
app.UseStaticFiles();
app.MapControllers();

app.Run();
