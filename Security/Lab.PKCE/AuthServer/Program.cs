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

    var cspDisabled = context.Request.Path.Equals("/emulator.html", StringComparison.OrdinalIgnoreCase)
                      && string.Equals(context.Request.Query["csp"], "off", StringComparison.OrdinalIgnoreCase);

    if (!cspDisabled)
    {
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
