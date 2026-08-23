using Elastic.Clients.Elasticsearch;
using EsDailyLogs.Models;
using EsDailyLogs.Services;
using EsDailyLogsApi.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EsDailyLogsApi.Tests;

[Collection("Elasticsearch")]
public class TraditionalLogServiceIntegrationTests
{
    private readonly ElasticsearchClient _client;
    private readonly TraditionalLogService _service;

    public TraditionalLogServiceIntegrationTests(ElasticsearchFixture fixture)
    {
        _client = fixture.Client;
        _service = new TraditionalLogService(_client, NullLogger<TraditionalLogService>.Instance);
    }

    [Fact]
    public async Task Write_And_Query_On_Traditional_Daily_Index_Should_Succeed()
    {
        var now = DateTime.UtcNow;
        var dailyIndex = $"logs-app-{now:yyyy.MM.dd}";

        var log = new LogEntry
        {
            Timestamp = now,
            Service = "traditional-test-service",
            Level = "Warning",
            Message = "Traditional daily index test message",
            TraceId = "trace-trad-001"
        };

        // 1. 寫入傳統每日索引
        var writeSuccess = await _service.WriteLogAsync(log);
        writeSuccess.Should().BeTrue();

        // 刷新索引
        await _client.Indices.RefreshAsync(dailyIndex);

        // 2. 跨日範圍查詢
        var logs = await _service.QueryLogsAsync(
            service: "traditional-test-service",
            keyword: "Traditional",
            from: now.AddHours(-1),
            to: now.AddHours(1),
            size: 10
        );

        logs.Should().NotBeEmpty();
        var targetLog = logs.First(l => l.TraceId == "trace-trad-001");
        targetLog.Message.Should().Be("Traditional daily index test message");
    }
}
