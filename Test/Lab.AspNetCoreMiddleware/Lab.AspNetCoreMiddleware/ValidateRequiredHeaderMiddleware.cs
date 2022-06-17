using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Lab.AspNetCoreMiddleware;

public class ValidateRequiredHeaderMiddleware
{
    private readonly string[] _requireHeaderNames =
    {
        HeaderNames.UserId,
        HeaderNames.Code,
    };

    private readonly RequestDelegate _next;

    public ValidateRequiredHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context,
        JsonSerializerOptions jsonSerializerOptions)
    {
        var failureResults = new List<FailureResult>();
        foreach (var name in this._requireHeaderNames)
        {
            if (context.Request.Headers.TryGetValue(name, out var value) == false)
            {
                failureResults.Add(new FailureResult
                {
                    Code = FailureCode.INVALID_FORMAT.ToString(),
                    PropertyName = name,
                    Messages = $"The '{name}' header is required.",
                });
            }
            else
            {
                if (name == HeaderNames.Code)
                {
                    if (long.TryParse(value, out var code) == false)
                    {
                        failureResults.Add(new FailureResult
                        {
                            Code = FailureCode.INVALID_TYPE.ToString(),
                            PropertyName = name,
                            Value = value,
                            Messages = $"'{value}' not numbers",
                        });
                    }
                }
            }
        }

        if (failureResults.Count > 0)
        {
            var failure = new Failure
            {
                Code = FailureCode.INVALID_REQUEST.ToString(),
                Messages = failureResults
            };
            var failureJson = JsonSerializer.Serialize(failure, jsonSerializerOptions);
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(failureJson, Encoding.UTF8, context.RequestAborted);
            return;
        }

        await this._next(context);
    }
}