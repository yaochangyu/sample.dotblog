using System.Text.Encodings.Web;
using System.Text.Json;
using Lab.RefitClient.GeneratedCode.PetStore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Refit;

namespace Lab.RefitClient.TestProject;

public static class HttpClientExtensions
{
    public static void AddHeaders(this HttpRequestMessage requestMessage, Dictionary<string, string> headers)
    {
        foreach (var header in headers)
        {
            requestMessage.Headers.Add(header.Key, header.Value);
        }
    }
}

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public async Task 自動化測試()
    {
        var server = new PetStoreTestServer();
        var httpClient = server.CreateClient();
        httpClient.DefaultRequestHeaders.Add(PetStoreHeaderNames.IdempotencyKey, "1234567890");
        httpClient.DefaultRequestHeaders.Add(PetStoreHeaderNames.ApiKey, "1234567890");
        httpClient.BaseAddress = new Uri(httpClient.BaseAddress, "api/v3");
        var client = RestService.For<ISwaggerPetstoreOpenAPI30>(httpClient);
        var username = "yao";

        var response = await client.GetUserByName(username);
        var content = response.Content;
        Assert.AreEqual(username, content.Username);
    }

    [TestMethod]
    public async Task 手動建立API()
    {
        var baseUrl = "https://localhost:7285/api/v3";
        var services = new ServiceCollection();
        services.AddRefitClient<ISwaggerPetstoreOpenAPI30>()
            .ConfigureHttpClient(p =>
            {
                p.DefaultRequestHeaders.Add(PetStoreHeaderNames.IdempotencyKey, "1234567890");
                p.DefaultRequestHeaders.Add(PetStoreHeaderNames.ApiKey, "1234567890");
                p.BaseAddress = new Uri(baseUrl);
            });
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetService<ISwaggerPetstoreOpenAPI30>();
        var username = "yao";
        var response = await client.GetUserByName(username);
        var content = response.Content;
        Assert.AreEqual(username, content.Username);
    }
}