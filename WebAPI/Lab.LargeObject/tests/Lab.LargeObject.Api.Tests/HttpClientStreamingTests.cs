using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lab.LargeObject.Api.Tests;

public class HttpClientStreamingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public HttpClientStreamingTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StreamReadings_ClientReceivesViaIAsyncEnumerable_CorrectCount()
    {
        // Arrange
        var count = 0;
        double sum = 0;

        // Act: Client 端以串流邊收邊算，記憶體中不建立 524k 元素陣列
        await foreach (var item in _client.GetFromJsonStreamingAsync<double>("/api/export-readings-stream"))
        {
            count++;
            sum += item;
        }

        // Assert
        Assert.Equal(524288, count);
        Assert.True(sum > 0);
    }

    [Fact]
    public async Task StreamStrings_ClientReceivesViaIAsyncEnumerable_CorrectCount()
    {
        // Arrange
        var count = 0;
        long totalLength = 0;

        // Act: Client 端逐筆消費 50k 字串
        await foreach (var item in _client.GetFromJsonStreamingAsync<string>("/api/export-strings-stream"))
        {
            count++;
            totalLength += item.Length;
        }

        // Assert
        Assert.Equal(50000, count);
        Assert.True(totalLength > 0);
    }

    [Fact]
    public async Task StreamMembersStruct_ClientReceivesViaIAsyncEnumerable_CorrectData()
    {
        // Arrange
        var count = 0;
        var activeCount = 0;

        // Act: Client 端逐筆處理 20k 筆 Struct
        await foreach (var member in _client.GetFromJsonStreamingAsync<MemberAccount>("/api/export-members-stream", JsonOptions))
        {
            count++;
            if (member.Status == MemberStatus.Active)
            {
                activeCount++;
            }
        }

        // Assert
        Assert.Equal(20000, count);
        Assert.True(activeCount > 0);
    }

    [Fact]
    public async Task StreamMembersClass_ClientReceivesViaIAsyncEnumerable_CorrectData()
    {
        // Arrange
        var count = 0;
        var activeCount = 0;

        // Act: Client 端逐筆處理 20k 筆 Class
        await foreach (var member in _client.GetFromJsonStreamingAsync<MemberAccountClass>("/api/export-members-class-stream", JsonOptions))
        {
            count++;
            if (member.Status == MemberStatus.Active)
            {
                activeCount++;
            }
        }

        // Assert
        Assert.Equal(20000, count);
        Assert.True(activeCount > 0);
    }
}
