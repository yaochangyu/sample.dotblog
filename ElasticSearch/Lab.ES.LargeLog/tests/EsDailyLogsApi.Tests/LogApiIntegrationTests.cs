using System.Net;
using System.Net.Http.Json;
using EsDailyLogs.Models;
using EsDailyLogsApi.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace EsDailyLogsApi.Tests;

[Collection("Elasticsearch")]
public class LogApiIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LogApiIntegrationTests(ElasticsearchFixture fixture)
    {
        _factory = new CustomWebApplicationFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Post_Log_Should_Return_Accepted_202()
    {
        var entry = new LogEntry
        {
            Service = "webapi-test-service",
            Level = "Information",
            Message = "Testing WebApi endpoint",
            TraceId = "trace-api-001"
        };

        var response = await _client.PostAsJsonAsync("/api/logs", entry);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Post_DailyIndex_Log_Should_Return_Created_201()
    {
        var entry = new LogEntry
        {
            Service = "webapi-daily-test-service",
            Level = "Warning",
            Message = "Testing DailyIndex WebApi endpoint",
            TraceId = "trace-api-daily-001"
        };

        var response = await _client.PostAsJsonAsync("/api/daily-index/logs", entry);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
