using System.Net;
using System.Net.Http.Json;
using EsDailyLogs.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EsDailyLogsApi.Tests;

public class LogApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public LogApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
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
}
