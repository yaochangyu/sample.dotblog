using Elastic.Clients.Elasticsearch;
using EsDailyLogsApi.Tests.Fixtures;
using Reqnroll;
using Reqnroll.BoDi;

namespace EsDailyLogsApi.Tests.Hooks;

[Binding]
public class TestHooks
{
    private static ElasticsearchFixture _esFixture = null!;
    private static CustomWebApplicationFactory _factory = null!;
    private readonly IObjectContainer _container;

    public TestHooks(IObjectContainer container)
    {
        _container = container;
    }

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        _esFixture = new ElasticsearchFixture();
        await _esFixture.InitializeAsync();
        _factory = new CustomWebApplicationFactory(_esFixture.ConnectionString);
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
        if (_esFixture != null)
        {
            await _esFixture.DisposeAsync();
        }
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        _container.RegisterInstanceAs(_esFixture);
        _container.RegisterInstanceAs(_esFixture.Client);
        _container.RegisterInstanceAs(_factory);
        _container.RegisterInstanceAs(_factory.CreateClient());
    }
}
