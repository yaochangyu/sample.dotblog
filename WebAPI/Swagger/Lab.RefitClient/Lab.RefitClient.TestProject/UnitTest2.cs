using Lab.RefitClient.GeneratedCode.PetStore;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Lab.RefitClient.TestProject;

[TestClass]
public class UnitTest2
{
    [TestMethod]
    public async Task RestServiceFor()
    {
        var server = new PetStoreTestServer();
        var httpClient = server.CreateClient();
        httpClient.BaseAddress = new Uri(httpClient.BaseAddress, "api/v3");

        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () =>
            {
                var contextAccessor = new ContextAccessor<HeaderContext>();
                contextAccessor.Set(new HeaderContext
                {
                    IdempotencyKey = "1234567890",
                    ApiKey = "1234567890"
                });
                return new DefaultHeaderHandler(contextAccessor)
                {
                    InnerHandler = new SocketsHttpHandler()
                };
            },
        };
        var client = RestService.For<ISwaggerPetstoreOpenAPI30>(httpClient, settings);

        var username = "yao";
        var response = await client.GetUserByName(username);
        var content = response.Content;
        Assert.AreEqual(username, content.Username);
    }

    [TestMethod]
    public async Task AddRefitClient()
    {
        var baseUrl = "https://localhost:7285/api/v3";

        var services = new ServiceCollection();
        
        services.AddSingleton<ContextAccessor<HeaderContext>>();
        services.AddSingleton<IContextSetter<HeaderContext>>(p => p.GetService<ContextAccessor<HeaderContext>>());
        services.AddSingleton<IContextGetter<HeaderContext>>(p =>
        {
            var context = p.GetService<ContextAccessor<HeaderContext>>();
            context.Set(new HeaderContext
            {
                IdempotencyKey = "1234567890",
                ApiKey = "1234567890"
            });
            return context;
        });

        services.AddRefitClient<ISwaggerPetstoreOpenAPI30>()
            .ConfigureHttpClient(p => { p.BaseAddress = new Uri(baseUrl); })
            .AddHttpMessageHandler(p => new DefaultHeaderHandler(p.GetService<IContextGetter<HeaderContext>>()));

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetService<ISwaggerPetstoreOpenAPI30>();
        var username = "yao";
        var response = await client.GetUserByName(username);
        var content = response.Content;
        Assert.AreEqual(username, content.Username);
    }
}