using Lab.RefitClient.GeneratedCode.PetStore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Refit;

namespace Lab.RefitClient.TestProject;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public async Task 自動化測試()
    {
        var server = new PetStoreTestServer();
        var httpClient = server.CreateClient();
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
            .ConfigureHttpClient(c => { c.BaseAddress = new Uri(baseUrl); });
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetService<ISwaggerPetstoreOpenAPI30>();
        var username = "yao";
        var response = await client.GetUserByName(username);
        var content = response.Content;
        Assert.AreEqual(username, content.Username);
    }
}