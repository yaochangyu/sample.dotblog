using Xunit;

namespace EsDailyLogsApi.Tests.Fixtures;

[CollectionDefinition("Elasticsearch")]
public class ElasticsearchCollection : ICollectionFixture<ElasticsearchFixture>
{
}
