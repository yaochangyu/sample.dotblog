using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lab.LargeObject.Api.Tests;

public class StringEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public StringEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StringsList_ReturnsSummary()
    {
        var payload = new List<string> { "hello", "world", "dotblog" };
        var response = await _client.PostAsJsonAsync("/api/strings-list", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<StringsSummary>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(17, result.TotalLength); // 5 + 5 + 7
    }

    [Fact]
    public async Task StringsPooled_ReturnsSummary()
    {
        var payload = new List<string> { "hello", "world", "dotblog" };
        var response = await _client.PostAsJsonAsync("/api/strings", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<StringsSummary>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(17, result.TotalLength);
    }

    [Fact]
    public async Task StringsStream_ReturnsSummary()
    {
        var payload = new List<string> { "hello", "world", "dotblog" };
        var response = await _client.PostAsJsonAsync("/api/strings-stream", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<StringsSummary>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(17, result.TotalLength);
    }
}
