using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.IntegrateTest;

[TestClass]
public class BasicAuthenticationMiddleware整合測試
{
    [TestMethod]
    public void 訪問不需要授權的服務()
    {
        var server = new TestServer();
        var httpClient = server.CreateClient();
        var url = "demo";
        var response = httpClient.GetAsync(url).Result;
        var result = response.Content.ReadAsStringAsync().Result;
        Console.WriteLine(result);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public void 訪問受保護的服務()
    {
        var server = new TestServer();
        var httpClient = server.CreateClient();
        var url = "user";
        var clientId = "YAO";
        var clientSecret = "9527";
        using var requestMessage = CreateBasicAuthenticationRequest(url, clientId, clientSecret);
        var response = httpClient.SendAsync(requestMessage).Result;
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public void 訪問受保護的服務_驗證失敗()
    {
        var server = new TestServer();
        var httpClient = server.CreateClient();
        var url = "user";
        var clientId = "YAO1234";
        var clientSecret = "9527";
        using var requestMessage = CreateBasicAuthenticationRequest(url, clientId, clientSecret);
        var response = httpClient.SendAsync(requestMessage).Result;
        response.Headers.TryGetValues("WWW-Authenticate", out var values);
        Console.WriteLine($"驗證失敗：{values.First()}");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static HttpRequestMessage CreateBasicAuthenticationRequest(string url, string clientId, string clientSecret)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        var authenticationString = $"{clientId}:{clientSecret}";
        var base64Encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("basic", base64Encoded);
        return requestMessage;
    }
}