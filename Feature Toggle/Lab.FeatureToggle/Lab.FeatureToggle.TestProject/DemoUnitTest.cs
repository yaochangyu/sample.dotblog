using Lab.FeatureToggle.WebAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace Lab.FeatureToggle.TestProject;

[TestClass]
public class DemoUnitTest
{
    [TestMethod]
    public async Task CreateFeatureA()
    {
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");
        var services = new ServiceCollection();
        services.AddSingleton<Demo>();
        services.AddFeatureManagement(configBuilder.Build());
        var serviceProvider = services.BuildServiceProvider();
        var target = serviceProvider.GetService<Demo>();
        var actual = await target.CreateFeatureA();
        Assert.AreEqual("OK", actual);
    }

    [TestMethod]
    public async Task CreateFeatureB()
    {
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");
        var services = new ServiceCollection();
        services.AddSingleton<Demo>();
        services.AddFeatureManagement(configBuilder.Build());
        var serviceProvider = services.BuildServiceProvider();
        var target = serviceProvider.GetService<Demo>();
        var actual = await target.CreateFeatureB();
        Assert.AreEqual(null, actual);
    }
}