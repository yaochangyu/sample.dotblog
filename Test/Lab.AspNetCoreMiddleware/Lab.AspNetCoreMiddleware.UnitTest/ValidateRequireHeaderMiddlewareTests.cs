using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.JsonDiffPatch.MsTest;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.AspNetCoreMiddleware.UnitTest;

[TestClass]
public class ValidateRequireHeaderMiddlewareTests
{
    [TestMethod]
    public async Task HeaderCode型別錯誤會驗證失敗()
    {
        var expected = @"
{
  ""code"": ""INVALID_REQUEST"",
  ""messages"": [
    {
      ""code"": ""INVALID_TYPE"",
      ""propertyName"": ""X-Code"",
      ""messages"": ""'abc' not numbers"",
      ""value"": ""abc""
    }
  ]
}
";
        using var testServer = await CreateTestServer();
        var httpContext = await testServer.SendAsync(context =>
        {
            context.Request.Headers[HeaderNames.UserId] = "yao";
            context.Request.Headers[HeaderNames.Code] = "abc";
        });
        var response = httpContext.Response;
        var stream = response.Body;

        var actual = await new StreamReader(stream).ReadToEndAsync();
        Assert.That.JsonAreEqual(expected, actual, true);
    }

    [TestMethod]
    public async Task 所有Header為空會驗證失敗()
    {
        var expected = @"
{
  ""code"": ""INVALID_REQUEST"",
  ""messages"": [
    {
      ""code"": ""INVALID_FORMAT"",
      ""propertyName"": ""X-User-Id"",
      ""messages"": ""The 'X-User-Id' header is required.""
    },
    {
      ""code"": ""INVALID_FORMAT"",
      ""propertyName"": ""X-Code"",
      ""messages"": ""The 'X-Code' header is required.""
    }
  ]
}
";
        using var testServer = await CreateTestServer();
        var httpContext = await testServer.SendAsync(context => { });
        var response = httpContext.Response;
        var stream = response.Body;

        var actual = await new StreamReader(stream).ReadToEndAsync();
        Assert.That.JsonAreEqual(expected, actual, true);
    }

    private static async Task<TestServer> CreateTestServer()
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer()
                    .ConfigureServices(
                        services =>
                        {
                            services.AddSingleton(p => new JsonSerializerOptions
                            {
                                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                                PropertyNameCaseInsensitive = true,
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                WriteIndented = false,

                                // Encoder = JavaScriptEncoder.Create(UnicodeRanges.All, UnicodeRanges.All),
                                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                            });
                        })
                    .Configure(app => { app.UseMiddleware<ValidateRequiredHeaderMiddleware>(); });
            })
            .StartAsync();

        var server = host.GetTestServer();
        server.BaseAddress = new Uri("https://我真的是假的/不要打我的臉/");
        return server;
    }
}