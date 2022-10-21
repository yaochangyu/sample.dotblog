using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Plugins.Http.CSharp;
using TechTalk.SpecFlow;

namespace Lab.NBomberTest.steps;

[Binding]
public class test : Steps
{
    [Given(@"準備以下 Header 參數")]
    public void Given準備以下Header參數(Table table)
    {
        var headers = new Dictionary<string, string>();
        foreach (var row in table.Rows)
        {
            if (headers.ContainsKey(row["Key"]) == false)
            {
                headers.Add("key", row["Value"]);
            }
        }

        this.ScenarioContext.Set(headers, "headers");
    }

    [Given(@"準備 HttpRequest '(.*)', ""(.*)""")]
    public void Given準備HttpRequest(string httpMethod, string url)
    {
        var httpFactory = HttpClientFactory.Create();

        this.ScenarioContext.TryGetValue<Dictionary<string, string>>("headers", out var headers);
        var step = Step.Create($"{httpMethod}-{url}",
            httpFactory,
            async context =>
            {
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                // await context.Client.SendAsync(httpRequestMessage);
                var response = await context.Client.GetAsync(url, context.CancellationToken);

                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: (int)response.StatusCode)
                    : Response.Fail(statusCode: (int)response.StatusCode);
            });

        this.ScenarioContext.Set(step, "step");
    }

    [Then(@"執行測試")]
    public void Then執行測試()
    {
        var step = this.ScenarioContext.Get<IStep>("step");
        var scenario = ScenarioBuilder.CreateScenario("demo", step)
                .WithLoadSimulations(Simulation.InjectPerSec(50, TimeSpan.FromSeconds(60)))
            ;
        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }
}