using Microsoft.Extensions.DependencyInjection;
using Reqnroll.Microsoft.Extensions.DependencyInjection;

namespace Lab.DeviceFingerprint.IntegrationTests.Support;

public static class DependencyInjectionConfig
{
    [ScenarioDependencies]
    public static IServiceCollection CreateServices()
    {
        return new ServiceCollection();
    }
}
