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
        // 本地 file:// 頁面的 origin 是 "null"，credentials: include 不能用 AllowAnyOrigin
        policy.WithOrigins("null", "http://localhost", "http://127.0.0.1")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

var app = builder.Build();

app.UseCors();
// 移除 HTTPS 重導，避免 fetch 遇到 HTTP→HTTPS redirect 時安全機制自動丟棄 Authorization header
// app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapControllers();

app.Run();
