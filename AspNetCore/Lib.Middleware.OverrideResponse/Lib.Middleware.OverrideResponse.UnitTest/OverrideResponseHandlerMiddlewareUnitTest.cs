using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;

namespace Lib.Middleware.OverrideResponse.UnitTest;

[TestClass]
public class OverrideResponseHandlerMiddlewareUnitTest
{
    [TestMethod]
    public async Task LogTest()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(new JsonFormatter(), "log.txt")
            .WriteTo.Seq("http://localhost:5341")
            .CreateLogger();

        var exampleUser = new { Id = 1, Name = "Adam", Created = DateTime.Now };
        Log.Information("Created {@User} on {Created}", exampleUser, DateTime.Now);
        Log.CloseAndFlush();
    }

    [TestMethod]
    public async Task 不模糊訊息_1()
    {
        var expected = @"{""code"":""9527""}";

        using var host = await CreateHostAsync();
        var client = host.GetTestClient();
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"));
        var actual = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public async Task 不模糊訊息()
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
    public async Task 模糊化未授權訊息()
    {
        var expected = @"{""errorCode"":""NoAuthorization"",""errorMessage"":""Please contact your administrator""}";
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
                ErrorCode = "NoAuthorization",
                ErrorMessage = "No permission"
            }, StatusCodes.Status403Forbidden));

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
        return new JsonSerializerOptions
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
        services.AddSingleton(p => LoggerFactory.Create(builder =>
        {
            var logger = new LoggerConfiguration()

                    // .MinimumLevel.Debug()
                    // .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.Seq("http://localhost:5341")
                    .CreateBootstrapLogger()

                // .CreateLogger()
                ;
            builder.AddSerilog(logger, true);
        }));

        services.AddSingleton(p => p.GetService<ILoggerFactory>().CreateLogger<OverrideResponseHandlerMiddleware>());
        return services.BuildServiceProvider();
    }

    private static async Task<IHost> CreateHostAsync()
    {
        var host = await new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer()
                        .ConfigureServices(services =>
                        {
                            services.AddSingleton(p => CreateJsonSerializerOptions());
                            services.AddSingleton(p => LoggerFactory.Create(builder => { builder.AddConsole(); }));
                        })
                        .Configure(app =>
                        {
                            app.UseSerilogRequestLogging();
                            app.UseMiddleware<OverrideResponseHandlerMiddleware>();

                            app.Use(async (context, next) =>
                            {
                                await next.Invoke();
                                context.Response.StatusCode = StatusCodes.Status200OK;
                                context.Response.WriteAsJsonAsync(new { Code = "9527" });
                            });
                        })
                        ;
                })
                .UseSerilog((context, config) =>
                {
                    config.WriteTo.Seq("http://localhost:5341")
                        .WriteTo.Console()

                        // .WriteTo.File(new CompactJsonFormatter(), "log.txt")
                        .WriteTo.File(new RenderedCompactJsonFormatter(), "log.txt")
                        ;
                })
                .StartAsync()
            ;

        return host;
    }
}