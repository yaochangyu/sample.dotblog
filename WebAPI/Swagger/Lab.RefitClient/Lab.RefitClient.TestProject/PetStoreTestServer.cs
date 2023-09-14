using Lab.RefitClient.WebAPI;
using Lab.RefitClient.WebAPI.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lab.RefitClient.TestProject;

public class PetStoreTestServer : WebApplicationFactory<Program>
{
    private void ConfigureServices(IServiceCollection services)
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(this.ConfigureServices);
    }
}