using Lab.RefitClient.GeneratedCode.PetStore;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Lab.RefitClient.TestProject;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public async Task RestServiceFor()
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

    /// <summary>
    /// F5執行WebApi專案_再呼叫WebApi
    /// </summary>
    [TestMethod]
    public async Task AddRefitClient()
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