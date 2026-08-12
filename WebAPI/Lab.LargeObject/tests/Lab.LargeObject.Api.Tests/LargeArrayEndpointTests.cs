using System.Net.Http.Json;
using Lab.LargeObject.Api;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lab.LargeObject.Api.Tests;

public class LargeArrayEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public LargeArrayEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_Readings_接收超過LOH門檻的大陣列_回傳正確統計結果()
    {
        // Arrange：131072 個 double，序列化後 JSON body 約 1MB，確定會落在 LOH 門檻之上。
        const int elementCount = 131_072;
        var readings = new double[elementCount];
        for (var i = 0; i < elementCount; i++)
        {
            readings[i] = i + 0.5;
        }

        var expectedSum = readings.Sum();
        var expectedAverage = expectedSum / elementCount;

        // Act
        var response = await _client.PostAsJsonAsync("/api/readings", readings);

        // Assert
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<ReadingsSummary>();

        Assert.NotNull(summary);
        Assert.Equal(elementCount, summary!.Count);
        Assert.Equal(expectedSum, summary.Sum, precision: 6);
        Assert.Equal(expectedAverage, summary.Average, precision: 6);
    }

    [Fact]
    public async Task Post_Readings_空陣列_回傳Count為0且Average為0()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/readings", Array.Empty<double>());

        // Assert
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<ReadingsSummary>();

        Assert.NotNull(summary);
        Assert.Equal(0, summary!.Count);
        Assert.Equal(0, summary.Sum);
        Assert.Equal(0, summary.Average);
    }
}
