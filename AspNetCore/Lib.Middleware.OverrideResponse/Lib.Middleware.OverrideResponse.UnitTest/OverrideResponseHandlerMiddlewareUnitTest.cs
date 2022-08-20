using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lib.Middleware.OverrideResponse.UnitTest;

[TestClass]
public class OverrideResponseHandlerMiddlewareUnitTest
{
    [TestMethod]
    public async Task 不模糊內部訊息()
    {
        var expected = @"{""code"":""9527""}";

        var serviceProvider = CreateServiceProvider();
        var jsonSerializerOptions = serviceProvider.GetService<JsonSerializerOptions>();
        var logger = serviceProvider.GetService<ILogger<OverrideResponseHandlerMiddleware>>();
        var target = new OverrideResponseHandlerMiddleware(nextContext =>
            CreateFakeNextContext(nextContext, new { Code = "9527" }, StatusCodes.Status200OK));

        var httpContext = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };

        await target.InvokeAsync(httpContext, logger, jsonSerializerOptions);
        var response = httpContext.Response;
        var stream = response.Body;
        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        var actual = await new StreamReader(stream).ReadToEndAsync();
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public async Task 模糊化未驗證訊息()
    {
        var expected = @"{""errorCode"":""NoAuthentication"",""errorMessage"":""Please contact your administrator""}";
        var httpContext = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };
        var serviceProvider = CreateServiceProvider();
        var jsonSerializerOptions = serviceProvider.GetService<JsonSerializerOptions>();
        var logger = serviceProvider.GetService<ILogger<OverrideResponseHandlerMiddleware>>();

        var target = new OverrideResponseHandlerMiddleware(nextContext =>
            CreateFakeNextContext(nextContext, new
            {
                ErrorCode = "NoAuthentication",
                ErrorMessage = "Invalid userid or password"
            }, StatusCodes.Status401Unauthorized));

        await target.InvokeAsync(httpContext, logger, jsonSerializerOptions);

        var response = httpContext.Response;
        var stream = response.Body;
        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        var actual = await new StreamReader(stream).ReadToEndAsync();
        Assert.AreEqual(expected, actual);
    }

    private static Task CreateFakeNextContext(HttpContext context, object detailFailure, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.WriteAsJsonAsync(detailFailure);
        return Task.CompletedTask;
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        return new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin,
                UnicodeRanges.CjkUnifiedIdeographs),
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    private static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(p => CreateJsonSerializerOptions());
        services.AddSingleton(p => LoggerFactory.Create(builder => { builder.AddConsole(); }));
        services.AddSingleton(p => p.GetService<ILoggerFactory>().CreateLogger<OverrideResponseHandlerMiddleware>());
        return services.BuildServiceProvider();
    }
}