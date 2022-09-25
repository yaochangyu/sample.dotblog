using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.UnitTest;

[TestClass]
public class BasicAuthenticationHandler單元測試
{
    [TestMethod]
    public async Task 驗證成功()
    {
        var context = new DefaultHttpContext();
        var authorizationHeader = new StringValues(CreateBasicAuthenticationValue("yao", "9527"));
        context.Request.Headers.Add(HeaderNames.Authorization, authorizationHeader);

        using var testHost = await CreateTestHost();
        var handler = testHost.Services.GetService<BasicAuthenticationHandler>();
        await handler.InitializeAsync(new AuthenticationScheme("basic",
                "basic",
                typeof(BasicAuthenticationHandler)),
            context);
        var result = await handler.AuthenticateAsync();

        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public async Task 驗證失敗()
    {
        var context = new DefaultHttpContext();
        var authorizationHeader = new StringValues(string.Empty);
        context.Request.Headers.Add(HeaderNames.Authorization, authorizationHeader);

        using var testHost = await CreateTestHost();
        var handler = testHost.Services.GetService<BasicAuthenticationHandler>();
        await handler.InitializeAsync(new AuthenticationScheme("basic",
                "basic",
                typeof(BasicAuthenticationHandler)),
            context);
        var result = await handler.AuthenticateAsync();

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("Invalid authorization Header", result.Failure.Message);
    }

    [TestMethod]
    public async Task 驗證失敗後回應錯誤()
    {
        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };

        var authorizationHeader = new StringValues(CreateBasicAuthenticationValue("yao123", "9527"));
        context.Request.Headers.Add(HeaderNames.Authorization, authorizationHeader);

        using var testHost = await CreateTestHost();
        var handler = testHost.Services.GetService<BasicAuthenticationHandler>();
        await handler.InitializeAsync(new AuthenticationScheme("basic",
                "basic",
                typeof(BasicAuthenticationHandler)),
            context);
        var authenticateResult = await handler.AuthenticateAsync();
        await handler.ChallengeAsync(authenticateResult.Properties);
        var response = context.Response;

        Assert.IsFalse(authenticateResult.Succeeded);
        var expected = "Basic realm=\"Demo Site\", charset=\"UTF-8\"";
        Assert.AreEqual(expected, response.Headers.WWWAuthenticate.ToString());
    }

    private static string CreateBasicAuthenticationValue(string userId, string password)
    {
        var certificate = $"{userId}:{password}";
        var base64Encode = Convert.ToBase64String(Encoding.ASCII.GetBytes(certificate));
        return $"Basic {base64Encode}";
    }

    private static async Task<HttpClient> CreateTestClient()
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer()
                    .ConfigureServices(
                        services =>
                        {
                            services.AddBasicAuthentication<BasicAuthenticationProvider>(_ => { });
                            services.AddAuthorization();
                        })
                    .Configure(app =>
                    {
                        app.UseAuthentication();
                        app.UseAuthorization();
                    });
            })
            .StartAsync();

        return host.GetTestClient();
    }

    private static async Task<IHost> CreateTestHost()
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer()
                    .ConfigureServices(
                        services =>
                        {
                            services.AddBasicAuthentication<BasicAuthenticationProvider>(_ => { });
                            services.AddAuthorization();
                        })
                    .Configure(app =>
                    {
                        app.UseAuthentication();
                        app.UseAuthorization();
                    });
            })
            .StartAsync();
        return host;
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
                            services.AddBasicAuthentication<BasicAuthenticationProvider>(_ => { });
                            services.AddAuthorization();
                        })
                    .Configure(app =>
                    {
                        app.UseAuthentication();
                        app.UseAuthorization();
                    });
            })
            .StartAsync();

        var server = host.GetTestServer();
        server.BaseAddress = new Uri("https://我真的是假的/不要打我的臉/");
        return server;
    }
}