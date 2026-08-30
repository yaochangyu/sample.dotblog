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

    [Fact]
    public async Task Post_ReadingsList_接收超過LOH門檻的大List_回傳正確統計結果()
    {
        // Arrange：131072 個 double（8 bytes * 131072 = 1,048,576 bytes，大於 85,000 bytes 門檻）
        const int elementCount = 131_072;
        var readings = new double[elementCount];
        for (var i = 0; i < elementCount; i++)
        {
            readings[i] = i + 0.5;
        }

        var expectedSum = readings.Sum();
        var expectedAverage = expectedSum / elementCount;

        // Act
        var response = await _client.PostAsJsonAsync("/api/readings-list", readings);

        // Assert
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<ReadingsSummary>();

        Assert.NotNull(summary);
        Assert.Equal(elementCount, summary!.Count);
        Assert.Equal(expectedSum, summary.Sum, precision: 6);
        Assert.Equal(expectedAverage, summary.Average, precision: 6);
    }

    [Fact]
    public async Task Post_ReadingsList_相較於Pooled端點_List每次請求皆配置超過1MB記憶體()
    {
        const int elementCount = 131_072;
        var readings = new double[elementCount];
        for (var i = 0; i < elementCount; i++)
        {
            readings[i] = i + 0.5;
        }

        // 先做暖機請求
        await _client.PostAsJsonAsync("/api/readings", readings);
        await _client.PostAsJsonAsync("/api/readings-list", readings);

        // 測試 List 端點的記憶體配置量
        var allocBeforeList = GC.GetTotalAllocatedBytes(precise: true);
        var responseList = await _client.PostAsJsonAsync("/api/readings-list", readings);
        responseList.EnsureSuccessStatusCode();
        var allocAfterList = GC.GetTotalAllocatedBytes(precise: true);
        var listAllocated = allocAfterList - allocBeforeList;

        // 131,072 個 double 的 List 在反序列化時，底層陣列擴容（最終陣列 1MB，加上前面 512KB、256KB 等），
        // 單次請求累積配置至少大於 1MB (1,048,576 bytes)
        Assert.True(listAllocated >= 1_048_576, $"List 端點單次請求配置量應大於 1MB (實際為 {listAllocated:N0} bytes)");
    }

    [Fact]
    public async Task Post_ReadingsStream_串流解析大陣列_回傳正確統計結果()
    {
        const int elementCount = 131_072;
        var readings = new double[elementCount];
        for (var i = 0; i < elementCount; i++)
        {
            readings[i] = i + 0.5;
        }

        var expectedSum = readings.Sum();
        var expectedAverage = expectedSum / elementCount;

        // Act
        var response = await _client.PostAsJsonAsync("/api/readings-stream", readings);

        // Assert
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<ReadingsSummary>();

        Assert.NotNull(summary);
        Assert.Equal(elementCount, summary!.Count);
        Assert.Equal(expectedSum, summary.Sum, precision: 6);
        Assert.Equal(expectedAverage, summary.Average, precision: 6);
    }

    [Fact]
    public async Task Post_Readings_三種寫法皆能正確處理百萬級大型資料()
    {
        const int elementCount = 131_072;
        var readings = new double[elementCount];
        for (var i = 0; i < elementCount; i++) readings[i] = i + 0.5;

        var expectedSum = readings.Sum();

        // 1. ArrayPool 接收
        var respPooled = await _client.PostAsJsonAsync("/api/readings", readings);
        respPooled.EnsureSuccessStatusCode();
        var summaryPooled = await respPooled.Content.ReadFromJsonAsync<ReadingsSummary>();
        Assert.NotNull(summaryPooled);
        Assert.Equal(elementCount, summaryPooled!.Count);
        Assert.Equal(expectedSum, summaryPooled.Sum, precision: 6);

        // 2. List 接收
        var respList = await _client.PostAsJsonAsync("/api/readings-list", readings);
        respList.EnsureSuccessStatusCode();
        var summaryList = await respList.Content.ReadFromJsonAsync<ReadingsSummary>();
        Assert.NotNull(summaryList);
        Assert.Equal(elementCount, summaryList!.Count);
        Assert.Equal(expectedSum, summaryList.Sum, precision: 6);

        // 3. Streaming 接收
        var respStream = await _client.PostAsJsonAsync("/api/readings-stream", readings);
        respStream.EnsureSuccessStatusCode();
        var summaryStream = await respStream.Content.ReadFromJsonAsync<ReadingsSummary>();
        Assert.NotNull(summaryStream);
        Assert.Equal(elementCount, summaryStream!.Count);
        Assert.Equal(expectedSum, summaryStream.Sum, precision: 6);
    }
}
