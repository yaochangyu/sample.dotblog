using System.Diagnostics;
using System.Text.Json;

namespace Lab.CSRF.WebApi.Middleware;

public class SecurityLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityLoggingMiddleware> _logger;

    public SecurityLoggingMiddleware(RequestDelegate next, ILogger<SecurityLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;

        if (requestPath.StartsWithSegments("/api/csrf"))
        {
            var logData = new
            {
                Timestamp = DateTime.UtcNow,
                Method = requestMethod,
                Path = requestPath.ToString(),
                IP = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                Referer = context.Request.Headers["Referer"].ToString(),
                HasCsrfToken = context.Request.Headers.ContainsKey("X-CSRF-TOKEN")
            };

            _logger.LogInformation("CSRF Request: {LogData}", JsonSerializer.Serialize(logData));
        }

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request failed: {Method} {Path}", requestMethod, requestPath);
            throw;
        }
        finally
        {
            stopwatch.Stop();

            if (context.Response.StatusCode >= 400)
            {
                var errorLog = new
                {
                    Timestamp = DateTime.UtcNow,
                    Method = requestMethod,
                    Path = requestPath.ToString(),
                    StatusCode = context.Response.StatusCode,
                    IP = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers["User-Agent"].ToString(),
                    Referer = context.Request.Headers["Referer"].ToString(),
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds
                };

                _logger.LogWarning("Failed Request: {ErrorLog}", JsonSerializer.Serialize(errorLog));
            }
        }
    }
}
