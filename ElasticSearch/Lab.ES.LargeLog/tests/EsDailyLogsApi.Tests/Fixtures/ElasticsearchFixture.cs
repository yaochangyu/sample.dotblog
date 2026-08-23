using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Testcontainers.Elasticsearch;
using Xunit;

namespace EsDailyLogsApi.Tests.Fixtures;

public class ElasticsearchFixture : IAsyncLifetime
{
    private readonly ElasticsearchContainer _container = new ElasticsearchBuilder("docker.elastic.co/elasticsearch/elasticsearch:8.17.0")
        .WithCleanUp(true)
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public ElasticsearchClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var settings = new ElasticsearchClientSettings(new Uri(ConnectionString))
            .Authentication(new BasicAuthentication("elastic", "changeme"))
            .ServerCertificateValidationCallback(CertificateValidations.AllowAll)
            .MaximumRetries(3)
            .RequestTimeout(TimeSpan.FromSeconds(30));

        Client = new ElasticsearchClient(settings);

        // 初始化 Data Stream 索引樣板
        await Client.Transport.RequestAsync<StringResponse>(
            Elastic.Transport.HttpMethod.PUT,
            "/_index_template/logs_template",
            PostData.String("{\"index_patterns\":[\"logs-app-*\"],\"data_stream\":{},\"priority\":500}")
        );
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
