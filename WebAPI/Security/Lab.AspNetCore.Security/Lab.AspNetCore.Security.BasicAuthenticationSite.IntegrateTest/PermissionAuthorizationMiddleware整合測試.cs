using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Lab.AspNetCore.Security.BasicAuthenticationSite.IntegrateTest.Controllers;
using Lab.AspNetCore.Security.BasicAuthenticationSite.Security.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.IntegrateTest;

[TestClass]
public class PermissionAuthorizationMiddleware整合測試
{
    [TestMethod]
    public async Task 訪問受保護的服務_授權成功()
    {
        var server = CreateTestServer();
        var httpClient = server.CreateClient();
        var url = "permission";
        var clientId = "YAO";
        var clientSecret = "9527";
        var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Headers = { Authorization = CreateAuthenticationHeaderValue(clientId, clientSecret) }
        };
        var response = httpClient.SendAsync(request).Result;
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(content);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task 訪問受保護的服務_授權失敗()
    {
        var server = CreateTestServer();
        var httpClient = server.CreateClient();
        var url = "permission";
        var clientId = "jojo";
        var clientSecret = "9527";
        var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Headers = { Authorization = CreateAuthenticationHeaderValue(clientId, clientSecret) }
        };
        var response = httpClient.SendAsync(request).Result;
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(content);
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateTestServer()
    {
        var server = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IBasicAuthenticationProvider, DefaultBasicAuthenticationProvider>();
                services.AddControllers()
                    .AddApplicationPart(typeof(TestController).Assembly);
            });
        });
        return server;
    }

    private static AuthenticationHeaderValue CreateAuthenticationHeaderValue(string clientId, string clientSecret)
    {
        var authenticationString = $"{clientId}:{clientSecret}";
        var base64Encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
        return new AuthenticationHeaderValue("basic", base64Encoded);
    }
}