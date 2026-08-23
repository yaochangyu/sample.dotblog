using Elastic.Clients.Elasticsearch;
using EsDailyLogs.Models;
using EsDailyLogs.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EsDailyLogsApi.Tests;

public class DailyIndexLogServiceIntegrationTests
{
    private readonly ElasticsearchClient _client;
    private readonly DailyIndexLogService _service;

    public DailyIndexLogServiceIntegrationTests()
    {
        var settings = new ElasticsearchClientSettings(new Uri("http://localhost:9200"));
        _client = new ElasticsearchClient(settings);
        _service = new DailyIndexLogService(_client, NullLogger<DailyIndexLogService>.Instance);
    }

    [Fact]
    public async Task Write_And_Query_On_Daily_Index_Should_Succeed()
    {
        var now = DateTime.UtcNow;
        var dailyIndex = $"logs-app-{now:yyyy.MM.dd}";

        var log = new LogEntry
        {
            Timestamp = now,
            Service = "daily-index-test-service",
            Level = "Warning",
            Message = "Daily index test message",
            TraceId = "trace-daily-001"
        };

        // 1. 寫入單日索引
        var writeSuccess = await _service.WriteLogAsync(log);
        writeSuccess.Should().BeTrue();

        // 刷新索引
        await _client.Indices.RefreshAsync(dailyIndex);

        // 2. 跨日範圍查詢
        var logs = await _service.QueryLogsAsync(
            service: "daily-index-test-service",
            keyword: "Daily",
            from: now.AddHours(-1),
            to: now.AddHours(1),
            size: 10
        );

        logs.Should().NotBeEmpty();
        var targetLog = logs.First(l => l.TraceId == "trace-daily-001");
        targetLog.Message.Should().Be("Daily index test message");
    }
}
