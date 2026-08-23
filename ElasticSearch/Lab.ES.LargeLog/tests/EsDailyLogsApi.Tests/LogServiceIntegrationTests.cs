using Elastic.Clients.Elasticsearch;
using EsDailyLogs.Models;
using EsDailyLogs.Services;
using EsDailyLogsApi.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EsDailyLogsApi.Tests;

[Collection("Elasticsearch")]
public class LogServiceIntegrationTests
{
    private readonly ElasticsearchClient _client;
    private readonly LogService _service;

    public LogServiceIntegrationTests(ElasticsearchFixture fixture)
    {
        _client = fixture.Client;
        _service = new LogService(_client, NullLogger<LogService>.Instance);
    }

    [Fact]
    public async Task Full_CRUD_Lifecycle_On_DataStream_Should_Succeed()
    {
        // 1. [Create] 寫入測試 Log
        var log = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Service = "integration-test-service",
            Level = "Information",
            Message = "Integration test lifecycle log",
            TraceId = "trace-unit-001"
        };

        var bulkRes = await _client.BulkAsync(b => b
            .Index(LogService.TargetDataStream)
            .CreateMany(new[] { log })
        );
        bulkRes.IsValidResponse.Should().BeTrue();

        // 等待索引 Refresh
        await _client.Indices.RefreshAsync(LogService.TargetDataStream);

        // 2. [Read] 依關鍵字與服務查詢
        var logs = await _service.QueryLogsAsync(
            service: "integration-test-service",
            keyword: "lifecycle",
            from: DateTime.UtcNow.AddMinutes(-5),
            to: DateTime.UtcNow.AddMinutes(5),
            size: 10
        );

        logs.Should().NotBeEmpty();
        var targetLog = logs.First(l => l.TraceId == "trace-unit-001");
        targetLog.Id.Should().NotBeNullOrEmpty();

        // 3. [Read Single] 依 ID 查單筆
        var singleLog = await _service.GetByIdAsync(targetLog.Id!);
        singleLog.Should().NotBeNull();
        singleLog!.Message.Should().Be("Integration test lifecycle log");

        // 4. 取得底層 Backing Index
        var dsRes = await _client.Indices.GetDataStreamAsync(LogService.TargetDataStream);
        var backingIndex = dsRes.DataStreams.First().Indices.First().IndexName;

        // 5. [Update] 更新訊息
        var updateSuccess = await _service.UpdateLogMessageAsync(
            backingIndex,
            targetLog.Id!,
            "Updated lifecycle log message"
        );
        updateSuccess.Should().BeTrue();

        await _client.Indices.RefreshAsync(LogService.TargetDataStream);

        // 驗證更新
        var updatedLog = await _service.GetByIdAsync(targetLog.Id!);
        updatedLog!.Message.Should().Be("Updated lifecycle log message");

        // 6. [Delete] 刪除 Log
        var deleteSuccess = await _service.DeleteLogAsync(backingIndex, targetLog.Id!);
        deleteSuccess.Should().BeTrue();

        await _client.Indices.RefreshAsync(LogService.TargetDataStream);

        // 驗證刪除後查不到
        var deletedLog = await _service.GetByIdAsync(targetLog.Id!);
        deletedLog.Should().BeNull();
    }
}
