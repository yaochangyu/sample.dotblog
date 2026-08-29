using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lab.LargeObject.Api.Tests;

public class ResponseEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly System.Text.Json.JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public ResponseEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ExportList_Returns20kMembers()
    {
        var response = await _client.GetAsync("/api/export-list");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<List<MemberAccount>>(Options);
        Assert.NotNull(list);
        Assert.Equal(20000, list.Count);
        Assert.Equal("member000000", list[0].Account);
    }

    [Fact]
    public async Task ExportBytes_Returns20kMembers()
    {
        var response = await _client.GetAsync("/api/export-bytes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<List<MemberAccount>>(Options);
        Assert.NotNull(list);
        Assert.Equal(20000, list.Count);
        Assert.Equal("member000000", list[0].Account);
    }

    [Fact]
    public async Task ExportPooled_Returns20kMembers()
    {
        var response = await _client.GetAsync("/api/export-pooled");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<List<MemberAccount>>(Options);
        Assert.NotNull(list);
        Assert.Equal(20000, list.Count);
        Assert.Equal("member000000", list[0].Account);
    }

    [Fact]
    public async Task ExportStream_Returns20kMembers()
    {
        var response = await _client.GetAsync("/api/export-stream");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<List<MemberAccount>>(Options);
        Assert.NotNull(list);
        Assert.Equal(20000, list.Count);
        Assert.Equal("member000000", list[0].Account);
    }
}
