using System.Net;
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
        var contextAccessor = new ContextAccessor<HeaderContext>();
        var server = new PetStoreTestServer();
        var httpClient = server.CreateDefaultClient(new DefaultHeaderHandler(contextAccessor)
        {
            InnerHandler = new SocketsHttpHandler()
        });
        httpClient.BaseAddress = new Uri(httpClient.BaseAddress, "api/v3");

        var client = RestService.For<ISwaggerPetstoreOpenAPI30>(httpClient);

        var username = "yao";
        this.SetHeaderContext(contextAccessor);
        var response = await client.GetUserByName(username);
        var content = response.Content;
        Console.WriteLine("get first headers: {0}", response.Headers);
        Assert.AreEqual(username, content.Username);
        Thread.Sleep(1000);
        
        this.SetHeaderContext(contextAccessor);
        var response1 = await client.GetUserByName(username);
        var content1 = response1.Content;
        Console.WriteLine("get second headers: {0}", response1.Headers);
        Assert.AreEqual(username, content1.Username);
    }

    void SetHeaderContext(IContextSetter<HeaderContext> setter)
    {
        var key = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss.fff");
        var headerContext = new HeaderContext
        {
            IdempotencyKey = key,
            ApiKey = key
        };
        setter.Set(headerContext);
    }
    
    [TestMethod]
    public async Task AddRefitClient()
    {
        var baseUrl = "https://localhost:7285/api/v3";

        var services = new ServiceCollection();

        services.AddSingleton<ContextAccessor<HeaderContext>>();
        services.AddSingleton<IContextSetter<HeaderContext>>(p => p.GetService<ContextAccessor<HeaderContext>>());
        services.AddSingleton<IContextGetter<HeaderContext>>(p => p.GetService<ContextAccessor<HeaderContext>>());
        services.AddSingleton(p =>
        {
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () =>
                    new DefaultHeaderHandler(p.GetService<IContextGetter<HeaderContext>>())
                    {
                        InnerHandler = new SocketsHttpHandler()
                    },
            };
            return settings;
        });

        services.AddRefitClient<ISwaggerPetstoreOpenAPI30>(p => p.GetRequiredService<RefitSettings>())
            .ConfigureHttpClient(p => { p.BaseAddress = new Uri(baseUrl); })
            ;

        var serviceProvider = services.BuildServiceProvider();
        var contextSetter = serviceProvider.GetService<IContextSetter<HeaderContext>>();
        var client = serviceProvider.GetService<ISwaggerPetstoreOpenAPI30>();
        var username = "yao";

        this.SetHeaderContext(contextSetter);
        var response = await client.GetUserByName(username);
        var content = response.Content;
        Console.WriteLine("get first headers: {0}", response.Headers);
        Assert.AreEqual(username, content.Username);
        Thread.Sleep(1000);
        
        this.SetHeaderContext(contextSetter);
        var response1 = await client.GetUserByName(username);
        var content1 = response1.Content;
        Console.WriteLine("get second headers: {0}", response1.Headers);
        Assert.AreEqual(username, content1.Username);
    }
}