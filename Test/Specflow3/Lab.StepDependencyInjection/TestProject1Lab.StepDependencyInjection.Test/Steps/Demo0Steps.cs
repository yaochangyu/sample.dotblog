using TechTalk.SpecFlow;

namespace TestProject1Lab.StepDependencyInjection.Test.Steps;

[Binding]
[Scope(Feature = "Demo0")]
public class Demo0Steps
{
    private readonly FileProvider _fileProvider;

    private ScenarioContext ScenarioContext { get; }

    public Demo0Steps(ScenarioContext scenarioContext, FileProvider fileProvider)
    {
        this.ScenarioContext = scenarioContext;
        this._fileProvider = fileProvider;
    }

    // [ScenarioDependencies]
    // public static IServiceCollection CreateServices()
    // {
    //     var services = new ServiceCollection();
    //     services.AddSingleton<FileProvider>();
    //     return services;
    // }

    [When(@"取得檔案路徑")]
    public void When取得檔案路徑()
    {
        var path = this._fileProvider.GetPath();
        this.ScenarioContext.Set(path, "actual");
    }

    [Then(@"預期得到 ""(.*)""")]
    public void Then預期得到(string expected)
    {
        var actual = this.ScenarioContext.Get<string>("actual");
        Assert.AreEqual(expected, actual);
    }
}