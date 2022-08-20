using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Lib.Middleware.OverrideResponse;

public class OverrideResponseHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public OverrideResponseHandlerMiddleware(RequestDelegate next)
    {
        this._next = next;
    }

    public async Task InvokeAsync(HttpContext context,
        ILogger<OverrideResponseHandlerMiddleware> logger,
        JsonSerializerOptions jsonSerializerOptions)
    {
        var srcStream = context.Response.Body;
        await using var destStream = new MemoryStream();
        context.Response.Body = destStream;

        await this._next(context);

        destStream.Seek(0, SeekOrigin.Begin);

        var realResponseBody = await new StreamReader(destStream).ReadToEndAsync();
        destStream.Seek(0, SeekOrigin.Begin);
        // await destStream.CopyToAsync(srcStream);
        context.Response.Body = srcStream;

        var fuzzyBody = context.Response.StatusCode switch
        {
            401 => CreateFuzzyBody("NoAuthentication"),
            403 => CreateFuzzyBody("NoAuthorization"),
            _ => null
        };

        if (fuzzyBody != null)
        {
            var json = JsonSerializer.Serialize(fuzzyBody, jsonSerializerOptions);
            await context.Response.WriteAsync(json);
        }
        else
        {
            await context.Response.WriteAsync(realResponseBody);
        }
    }

    private static object CreateFuzzyBody(string failureCode)
    {
        return new
        {
            ErrorCode = failureCode,
            ErrorMessage = "Please contact your administrator"
        };
    }
}