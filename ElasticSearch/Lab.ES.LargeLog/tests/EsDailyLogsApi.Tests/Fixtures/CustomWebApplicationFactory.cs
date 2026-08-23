using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace EsDailyLogsApi.Tests.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _esConnectionString;

    public CustomWebApplicationFactory(string esConnectionString)
    {
        _esConnectionString = esConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ElasticsearchClient));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            var settings = new ElasticsearchClientSettings(new Uri(_esConnectionString))
                .Authentication(new BasicAuthentication("elastic", "changeme"))
                .ServerCertificateValidationCallback(CertificateValidations.AllowAll)
                .MaximumRetries(3)
                .RequestTimeout(TimeSpan.FromSeconds(30));

            services.AddSingleton(new ElasticsearchClient(settings));
        });
    }
}
