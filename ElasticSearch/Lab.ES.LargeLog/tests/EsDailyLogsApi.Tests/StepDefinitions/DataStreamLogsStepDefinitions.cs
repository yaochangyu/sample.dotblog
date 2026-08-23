using System.Net;
using System.Net.Http.Json;
using Elastic.Clients.Elasticsearch;
using EsDailyLogs.Models;
using EsDailyLogs.Services;
using FluentAssertions;
using Reqnroll;

namespace EsDailyLogsApi.Tests.StepDefinitions;

[Binding]
public class DataStreamLogsStepDefinitions
{
    private readonly ElasticsearchClient _esClient;
    private readonly HttpClient _httpClient;
    private readonly ScenarioContext _scenarioContext;

    public DataStreamLogsStepDefinitions(
        ElasticsearchClient esClient,
        HttpClient httpClient,
        ScenarioContext scenarioContext)
    {
        _esClient = esClient;
        _httpClient = httpClient;
        _scenarioContext = scenarioContext;
    }

    [Given(@"我有一筆日誌資料:")]
    public void Given我有一筆日誌資料(Table table)
    {
        var row = table.Rows[0];
        var entry = new LogEntry
        {
            Service = row["Service"],
            Level = row["Level"],
            Message = row["Message"],
            TraceId = row["TraceId"]
        };
        _scenarioContext.Set(entry, "CurrentLogEntry");
    }

    [When(@"我發送 POST 請求至 ""(.*)"" 寫入該日誌")]
    public async Task When我發送POST請求至寫入該日誌(string endpoint)
    {
        var entry = _scenarioContext.Get<LogEntry>("CurrentLogEntry");
        var response = await _httpClient.PostAsJsonAsync(endpoint, entry);
        _scenarioContext.Set(response, "LastHttpResponse");
    }

    [Then(@"等待背景批次處理器將日誌寫入 Data Stream")]
    public async Task Then等待背景批次處理器將日誌寫入DataStream()
    {
        // LogBatchProcessor FlushInterval 為 500ms
        await Task.Delay(1200);
        await _esClient.Indices.RefreshAsync(LogService.TargetDataStream);
    }

    [Then(@"透過 Data Stream 全文檢索關鍵字 ""(.*)"" 應能查得該筆日誌")]
    public async Task Then透過DataStream全文檢索關鍵字應能查得該筆日誌(string keyword)
    {
        var response = await _httpClient.GetAsync($"/api/logs?keyword={Uri.EscapeDataString(keyword)}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var logs = await response.Content.ReadFromJsonAsync<List<LogEntry>>();
        logs.Should().NotBeNull();
        logs.Should().Contain(l => l.Message.Contains(keyword));
    }

    [Given(@"Data Stream 中已存在一筆日誌:")]
    public async Task GivenDataStream中已存在一筆日誌(Table table)
    {
        var row = table.Rows[0];
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Service = row["Service"],
            Level = row["Level"],
            Message = row["Message"],
            TraceId = row["TraceId"]
        };

        var bulkResponse = await _esClient.BulkAsync(b => b
            .Index(LogService.TargetDataStream)
            .CreateMany(new[] { entry })
        );
        bulkResponse.IsValidResponse.Should().BeTrue();

        await _esClient.Indices.RefreshAsync(LogService.TargetDataStream);

        var item = bulkResponse.Items.First();
        var docId = item.Id!;
        var backingIndex = item.Index!;

        _scenarioContext.Set(docId, "ExistingLogId");
        _scenarioContext.Set(backingIndex, "ExistingLogBackingIndex");
        _scenarioContext.Set(entry, "ExistingLogEntry");
    }

    [When(@"我發送 GET 請求依日誌 ID 查詢該筆日誌")]
    public async Task When我發送GET請求依日誌ID查詢該筆日誌()
    {
        var logId = _scenarioContext.Get<string>("ExistingLogId");
        var response = await _httpClient.GetAsync($"/api/logs/{logId}");
        _scenarioContext.Set(response, "LastHttpResponse");
    }

    [Then(@"回傳的日誌內容訊息應為 ""(.*)""")]
    public async Task Then回傳的日誌內容訊息應為(string expectedMessage)
    {
        var response = _scenarioContext.Get<HttpResponseMessage>("LastHttpResponse");
        var log = await response.Content.ReadFromJsonAsync<LogEntry>();
        log.Should().NotBeNull();
        log!.Message.Should().Be(expectedMessage);
    }

    [When(@"我發送 PUT 請求至該日誌所屬底層索引更新訊息為 ""(.*)""")]
    public async Task When我發送PUT請求至該日誌所屬底層索引更新訊息為(string newMessage)
    {
        var logId = _scenarioContext.Get<string>("ExistingLogId");
        var backingIndex = _scenarioContext.Get<string>("ExistingLogBackingIndex");

        var updateReq = new UpdateLogRequest(newMessage);
        var response = await _httpClient.PutAsJsonAsync($"/api/logs/{backingIndex}/{logId}", updateReq);
        _scenarioContext.Set(response, "LastHttpResponse");

        await _esClient.Indices.RefreshAsync(LogService.TargetDataStream);
    }

    [Then(@"依 ID 重新取得日誌其訊息應更新為 ""(.*)""")]
    public async Task Then依ID重新取得日誌其訊息應更新為(string expectedMessage)
    {
        var logId = _scenarioContext.Get<string>("ExistingLogId");
        var response = await _httpClient.GetAsync($"/api/logs/{logId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var log = await response.Content.ReadFromJsonAsync<LogEntry>();
        log.Should().NotBeNull();
        log!.Message.Should().Be(expectedMessage);
    }

    [When(@"我發送 DELETE 請求至該日誌所屬底層索引刪除該筆日誌")]
    public async Task When我發送DELETE請求至該日誌所屬底層索引刪除該筆日誌()
    {
        var logId = _scenarioContext.Get<string>("ExistingLogId");
        var backingIndex = _scenarioContext.Get<string>("ExistingLogBackingIndex");

        var response = await _httpClient.DeleteAsync($"/api/logs/{backingIndex}/{logId}");
        _scenarioContext.Set(response, "LastHttpResponse");

        await _esClient.Indices.RefreshAsync(LogService.TargetDataStream);
    }

    [Then(@"依 ID 重新取得該日誌應回傳 HTTP 狀態碼 (.*)")]
    public async Task Then依ID重新取得該日誌應回傳HTTP狀態碼(int expectedStatusCode)
    {
        var logId = _scenarioContext.Get<string>("ExistingLogId");
        var response = await _httpClient.GetAsync($"/api/logs/{logId}");
        ((int)response.StatusCode).Should().Be(expectedStatusCode);
    }
}
