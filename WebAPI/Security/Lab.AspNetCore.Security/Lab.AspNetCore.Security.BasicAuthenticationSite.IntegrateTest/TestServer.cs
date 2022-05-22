using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.IntegrateTest;

public class TestServer : WebApplicationFactory<Program>
{
    private void ConfigureServices(IServiceCollection services)
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(this.ConfigureServices);
        builder.UseSetting("https_port", "9527");
    }
}