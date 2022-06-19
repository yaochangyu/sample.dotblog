using Lab.AspNetCore.Security.BasicAuthenticationSite.IntegrateTest.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.IntegrateTest;

public class TestServer : WebApplicationFactory<Program>
{
    private void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(TestController).Assembly);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(this.ConfigureServices)
            .UseSetting("https_port", "9527")

            // .UseUrls("https://localhost:9527")
            ;
    }
}