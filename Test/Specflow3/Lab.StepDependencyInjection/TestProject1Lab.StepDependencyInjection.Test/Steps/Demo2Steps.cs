using Microsoft.Extensions.DependencyInjection;
using SolidToken.SpecFlow.DependencyInjection;
using TechTalk.SpecFlow;

namespace TestProject1Lab.StepDependencyInjection.Test.Steps;

[Binding]
[Scope(Feature = "Demo2")]
public class Demo2Steps
{
    private readonly SysFileProvider _sysFileProvider;

    public Demo2Steps(SysFileProvider sysFileProvider, ScenarioContext scenarioContext)
    {
        this._sysFileProvider = sysFileProvider;
        this.ScenarioContext = scenarioContext;
    }

    private ScenarioContext ScenarioContext { get; }


    [ScenarioDependencies]
    public static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new SysFileProvider("fake provider"));
        return services;
    }

    [When(@"取得檔案路徑")]
    public void When取得檔案路徑()
    {
        var path = this._sysFileProvider.GetPath();
        this.ScenarioContext.Set(path, "actual");
    }

    [Then(@"預期得到 ""(.*)""")]
    public void Then預期得到(string expected)
    {
        var actual = this.ScenarioContext.Get<string>("actual");
        Assert.AreEqual(expected, actual);
    }
}