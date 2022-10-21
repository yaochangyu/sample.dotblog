using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Plugins.Http.CSharp;

var httpFactory = ClientFactory.Create(
    name: "http_factory",                         
    clientCount: 1,
    initClient: (number,context) => Task.FromResult(new HttpClient())
);

var step1 = Step.Create("1",
    clientFactory: HttpClientFactory.Create("1"),
    execute: async context =>
    {
        var response = await context.Client.GetAsync("http://test.k6.io", context.CancellationToken);

        return response.IsSuccessStatusCode
            ? Response.Ok(statusCode: (int)response.StatusCode)
            : Response.Fail(statusCode: (int)response.StatusCode);
    });
var scenario1 = ScenarioBuilder.CreateScenario("1", step1)
    .WithLoadSimulations(Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromSeconds(30)));


// var pingPluginConfig = PingPluginConfig.CreateDefault(new[] { "test.k6.io" });
// var pingPlugin = new PingPlugin(pingPluginConfig);
//
NBomberRunner
    .RegisterScenarios(scenario1)

    // .WithWorkerPlugins(pingPlugin)
    .Run();