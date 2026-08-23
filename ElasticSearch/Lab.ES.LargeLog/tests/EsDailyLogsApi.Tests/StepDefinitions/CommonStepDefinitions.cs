using Elastic.Clients.Elasticsearch;
using FluentAssertions;
using Reqnroll;

namespace EsDailyLogsApi.Tests.StepDefinitions;

[Binding]
public class CommonStepDefinitions
{
    private readonly ElasticsearchClient _esClient;
    private readonly HttpClient _httpClient;
    private readonly ScenarioContext _scenarioContext;

    public CommonStepDefinitions(
        ElasticsearchClient esClient,
        HttpClient httpClient,
        ScenarioContext scenarioContext)
    {
        _esClient = esClient;
        _httpClient = httpClient;
        _scenarioContext = scenarioContext;
    }

    [Given(@"Elasticsearch 服務已正常運作")]
    public async Task GivenElasticsearch服務已正常運作()
    {
        var pingResponse = await _esClient.PingAsync();
        pingResponse.IsValidResponse.Should().BeTrue("Elasticsearch 服務應正常回應 Ping");
    }

    [Given(@"系統 API 服務已啟動")]
    public void Given系統API服務已啟動()
    {
        _httpClient.Should().NotBeNull("HttpClient 應已建立連線");
    }

    [Then(@"API 應回傳 HTTP 狀態碼 (.*)")]
    public void ThenAPI應回傳HTTP狀態碼(int expectedStatusCode)
    {
        var lastResponse = _scenarioContext.Get<HttpResponseMessage>("LastHttpResponse");
        ((int)lastResponse.StatusCode).Should().Be(expectedStatusCode);
    }
}
