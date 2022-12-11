using BoDi;
using TechTalk.SpecFlow;

namespace Lab.StepDependencyInjection.Test.Steps;

[Binding]
[Scope(Feature = "Demo1")]
public class Demo1Steps : TechTalk.SpecFlow.Steps
{
    private readonly IObjectContainer objectContainer;

    private ScenarioContext ScenarioContext { get; }

    public Demo1Steps(IObjectContainer objectContainer,
        ScenarioContext scenarioContext)
    {
        objectContainer.RegisterInstanceAs(new FileProvider(), nameof(FileProvider));
        this.objectContainer = objectContainer;
        this.ScenarioContext = scenarioContext;
    }

    [When(@"取得檔案路徑")]
    public void When取得檔案路徑()
    {
        var target = this.objectContainer.Resolve<FileProvider>(nameof(FileProvider));
        var path = target.GetPath();
        this.ScenarioContext.Set(path, "actual");
    }

    [Then(@"預期得到 ""(.*)""")]
    public void Then預期得到(string expected)
    {
        var actual = this.ScenarioContext.Get<string>("actual");
        Assert.AreEqual(expected, actual);
    }
}