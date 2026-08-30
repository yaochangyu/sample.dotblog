using Microsoft.AspNetCore.Mvc.Testing;
using Reqnroll;
using Reqnroll.BoDi;

namespace Lab.LargeObject.Specs.Hooks;

[Binding]
public class TestHooks
{
    private static WebApplicationFactory<Program>? _factory;
    private readonly IObjectContainer _container;

    public TestHooks(IObjectContainer container)
    {
        _container = container;
    }

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        _factory = new WebApplicationFactory<Program>();
    }

    [AfterTestRun]
    public static void AfterTestRun()
    {
        _factory?.Dispose();
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        if (_factory != null)
        {
            var client = _factory.CreateClient();
            _container.RegisterInstanceAs(client);
        }
    }
}
