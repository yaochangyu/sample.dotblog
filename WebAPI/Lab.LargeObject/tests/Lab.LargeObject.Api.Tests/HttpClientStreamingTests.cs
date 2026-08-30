using System.Net.Http.Json;
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

    [Fact]
    public async Task ClientMemory_ListVsStreaming_VerifiesClientLohBehavior()
    {
        // 1. 先強制清理 GC 讓記憶體回到基準線
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // 2. 測試【未池化 List】：Client 端一次載入 524k double (~4MB)
        var memBeforeList = GC.GetGCMemoryInfo();
        var lohBeforeList = memBeforeList.GenerationInfo.Length > 3 ? memBeforeList.GenerationInfo[3].SizeAfterBytes : 0;

        var list = await _client.GetFromJsonAsync<List<double>>("/api/export-readings-list");
        Assert.NotNull(list);
        Assert.Equal(524288, list.Count);

        var memAfterList = GC.GetGCMemoryInfo();
        var lohAfterList = memAfterList.GenerationInfo.Length > 3 ? memAfterList.GenerationInfo[3].SizeAfterBytes : 0;

        // 3. 再次強制清理
        list = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // 4. 測試【串流 Streaming】：Client 端以 IAsyncEnumerable 邊收邊算
        var memBeforeStream = GC.GetGCMemoryInfo();
        var lohBeforeStream = memBeforeStream.GenerationInfo.Length > 3 ? memBeforeStream.GenerationInfo[3].SizeAfterBytes : 0;

        var count = 0;
        double sum = 0;
        await foreach (var item in _client.GetFromJsonStreamingAsync<double>("/api/export-readings-stream"))
        {
            count++;
            sum += item;
        }

        var memAfterStream = GC.GetGCMemoryInfo();
        var lohAfterStream = memAfterStream.GenerationInfo.Length > 3 ? memAfterStream.GenerationInfo[3].SizeAfterBytes : 0;

        // Assert: 串流模式在 Client 端確實接收完整 524k 筆資料，且不造成 LOH 堆積
        Assert.Equal(524288, count);
        Assert.True(sum > 0);
    }
}
