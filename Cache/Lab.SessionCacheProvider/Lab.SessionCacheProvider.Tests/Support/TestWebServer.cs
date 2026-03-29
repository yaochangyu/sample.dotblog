using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Lab.SessionCacheProvider.Tests.Support;

public class TestWebServer : WebApplicationFactory<TestWebApp>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var app = TestWebApp.CreateApp(
            Array.Empty<string>(),
            b => b.WebHost.UseTestServer());

        app.Start();
        return app;
    }
}
