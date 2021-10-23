using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Lab.Test.WebApi.Net5.TestProject
{
    public class CustomTestServer : WebApplicationFactory<Startup>
    {
        private void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped(p =>
            {
                var fileProvider = Substitute.For<IFileProvider>();
                fileProvider.Name().Returns("Fake FileProfile");
                return fileProvider;
            });
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(this.ConfigureServices);
        }
    }
}