using TechTalk.SpecFlow;

namespace Lab.StepDependencyInjection.Test.Steps;

[Binding]
public class Demo1Steps
{
    [When(@"呼叫 Web API")]
    public async Task When呼叫WebApi()
    {
        var testServer = new TestServer();
        var client = testServer.CreateClient();
        var response = await client.GetAsync("Default");
        var response1 = await client.GetAsync("Demo");
    }
}