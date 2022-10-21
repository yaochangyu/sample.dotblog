// using NBomber.Contracts;
// using NBomber.CSharp;
// using NBomber.Plugins.Http.CSharp;
//
// var httpFactory = HttpClientFactory.Create();
//
// var step = Step.Create("fetch_html_page",
//     clientFactory: httpFactory,
//     execute: async context =>
//     {
//         var response = await context.Client.GetAsync("https://test.k6.io/", context.CancellationToken);
//
//         return response.IsSuccessStatusCode
//             ? Response.Ok(statusCode: (int)response.StatusCode)
//             : Response.Fail(statusCode: (int)response.StatusCode);
//     });
//
// var scenario = ScenarioBuilder
//     .CreateScenario("simple_http", step)
//     .WithWarmUpDuration(TimeSpan.FromSeconds(5))
//     .WithLoadSimulations(new[]
//     {
//         Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromSeconds(30))
//     });
//
// NBomberRunner.RegisterScenarios(scenario).Run();