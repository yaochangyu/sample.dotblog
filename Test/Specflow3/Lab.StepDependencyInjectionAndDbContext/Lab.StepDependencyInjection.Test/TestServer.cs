using Lab.StepDependencyInjection.WebAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lab.StepDependencyInjection.Test;

public class TestServer : WebApplicationFactory<Program>
{
    private void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(p => p.AddConsole());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(this.ConfigureServices);
        builder.UseSetting("https_port", "9527");
    }
}