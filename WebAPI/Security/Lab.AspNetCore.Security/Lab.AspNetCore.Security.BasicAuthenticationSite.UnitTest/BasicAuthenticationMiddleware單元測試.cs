using System;
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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.UnitTest;

[TestClass]
public class BasicAuthenticationMiddleware單元測試
{
    [TestMethod]
    public async Task 驗證失敗()
    {
        using var server = await CreateTestServer();
        var httpContext = await server.SendAsync(config =>
        {
            config.Request.Headers.Authorization = CreateBasicAuthenticationValue("yao", "9527xxxx");
        });

        // 驗證失敗沒有觸發 BasicAuthenticationHandler.HandleChallengeAsync
        var userPrincipal = httpContext.User;
        Assert.AreEqual(false, userPrincipal.Identity.IsAuthenticated);
    }

    [TestMethod]
    public async Task 驗證成功()
    {
        using var server = await CreateTestServer();
        var httpContext = await server.SendAsync(config =>
        {
            config.Request.Headers.Authorization = CreateBasicAuthenticationValue("yao", "9527");
        });
        var userPrincipal = httpContext.User;
        Assert.AreEqual(true, userPrincipal.Identity.IsAuthenticated);
    }

    private static string CreateBasicAuthenticationValue(string userId, string password)
    {
        var certificate = $"{userId}:{password}";
        var base64Encode = Convert.ToBase64String(Encoding.ASCII.GetBytes(certificate));
        return $"Basic {base64Encode}";
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
                            services.AddSingleton<IBasicAuthenticationProvider, BasicAuthenticationProvider>();
                            services.AddBasicAuthentication(_ => { });
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