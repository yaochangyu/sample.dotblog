using System.Net;
using System.Net.Http.Json;
using Elastic.Clients.Elasticsearch;
using EsDailyLogs.Models;
using FluentAssertions;
using Reqnroll;

namespace EsDailyLogsApi.Tests.StepDefinitions;

[Binding]
public class DailyIndexLogsStepDefinitions
{
    private readonly ElasticsearchClient _esClient;
    private readonly HttpClient _httpClient;
    private readonly ScenarioContext _scenarioContext;

    public DailyIndexLogsStepDefinitions(
        ElasticsearchClient esClient,
        HttpClient httpClient,
        ScenarioContext scenarioContext)
    {
        _esClient = esClient;
        _httpClient = httpClient;
        _scenarioContext = scenarioContext;
    }

    [Given(@"我有一筆單日日誌資料:")]
    public void Given我有一筆單日日誌資料(Table table)
    {
        var row = table.Rows[0];
        var entry = new LogEntry
        {
            Service = row["Service"],
            Level = row["Level"],
            Message = row["Message"],
            TraceId = row["TraceId"]
        };
        _scenarioContext.Set(entry, "CurrentDailyIndexLogEntry");
    }

    [When(@"我發送 POST 請求至 ""(.*)"" 寫入該單日日誌")]
    public async Task When我發送POST請求至寫入該單日日誌(string endpoint)
    {
        var entry = _scenarioContext.Get<LogEntry>("CurrentDailyIndexLogEntry");
        var response = await _httpClient.PostAsJsonAsync(endpoint, entry);
        _scenarioContext.Set(response, "LastHttpResponse");
    }

    [Then(@"回傳內容應包含所建立的日誌資訊")]
    public async Task Then回傳內容應包含所建立的日誌資訊()
    {
        var response = _scenarioContext.Get<HttpResponseMessage>("LastHttpResponse");
        var created = await response.Content.ReadFromJsonAsync<LogEntry>();
        created.Should().NotBeNull();
        var original = _scenarioContext.Get<LogEntry>("CurrentDailyIndexLogEntry");
        created!.Service.Should().Be(original.Service);
        created.Message.Should().Be(original.Message);
    }

    [Given(@"單日索引中已寫入以下日誌:")]
    public async Task Given單日索引中已寫入以下日誌(Table table)
    {
        var writtenIndices = new HashSet<string>();

        foreach (var row in table.Rows)
        {
            int daysAgo = int.Parse(row["DaysAgo"]);
            var logDate = DateTime.UtcNow.AddDays(-daysAgo);
            var dailyIndex = $"logs-app-{logDate:yyyy.MM.dd}";

            var entry = new LogEntry
            {
                Timestamp = logDate,
                Service = row["Service"],
                Level = row["Level"],
                Message = row["Message"],
                TraceId = row["TraceId"]
            };

            var indexRes = await _esClient.IndexAsync(entry, idx => idx.Index(dailyIndex));
            indexRes.IsValidResponse.Should().BeTrue();

            writtenIndices.Add(dailyIndex);
        }

        foreach (var index in writtenIndices)
        {
            await _esClient.Indices.RefreshAsync(index);
        }
    }

    [When(@"我發送 GET 請求至 ""(.*)"" 查詢服務 ""(.*)"" 且關鍵字為 ""(.*)"" 涵蓋過去 (.*) 天範圍")]
    public async Task When我發送GET請求至查詢服務且關鍵字為涵蓋過去天範圍(string endpoint, string service, string keyword, int days)
    {
        var from = DateTime.UtcNow.AddDays(-days).AddHours(-1).ToString("o");
        var to = DateTime.UtcNow.AddMinutes(5).ToString("o");
        var url = $"{endpoint}?service={Uri.EscapeDataString(service)}&keyword={Uri.EscapeDataString(keyword)}&from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}";

        var response = await _httpClient.GetAsync(url);
        _scenarioContext.Set(response, "LastHttpResponse");
    }

    [Then(@"查詢結果應包含至少 (.*) 筆日誌")]
    public async Task Then查詢結果應包含至少筆日誌(int minCount)
    {
        var response = _scenarioContext.Get<HttpResponseMessage>("LastHttpResponse");
        var logs = await response.Content.ReadFromJsonAsync<List<LogEntry>>();
        logs.Should().NotBeNull();
        logs!.Count.Should().BeGreaterThanOrEqualTo(minCount);
    }
}
