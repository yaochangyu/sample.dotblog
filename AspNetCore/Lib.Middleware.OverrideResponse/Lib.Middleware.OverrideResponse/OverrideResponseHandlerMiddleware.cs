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
        var originalResponseBodyStream = context.Response.Body;
        await using var newResponseBodyStream = new MemoryStream();
        context.Response.Body = newResponseBodyStream;

        await this._next(context);

        newResponseBodyStream.Seek(0, SeekOrigin.Begin);
        var statusCode = context.Response.StatusCode;
        var fuzzyBody = statusCode switch
        {
            401 => CreateFuzzyBody("NoAuthentication"),
            403 => CreateFuzzyBody("NoAuthorization"),
            _ => null
        };

        if (fuzzyBody != null)
        {
            var fuzzyData = JsonSerializer.Serialize(fuzzyBody, jsonSerializerOptions);
            logger.LogInformation("Fuzzy data：{FuzzyData}", fuzzyData);

            var realData = await new StreamReader(newResponseBodyStream).ReadToEndAsync();
            logger.LogInformation("Read data：{RealData}", realData);

            context.Response.Body = originalResponseBodyStream;
            await context.Response.WriteAsync(fuzzyData);
        }
        else
        {
            await newResponseBodyStream.CopyToAsync(originalResponseBodyStream);
            context.Response.Body = originalResponseBodyStream;
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