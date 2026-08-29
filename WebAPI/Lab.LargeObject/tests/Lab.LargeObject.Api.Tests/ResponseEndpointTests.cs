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

    // --- 1. 原生數值 double (524,288 筆) ---
    [Fact]
    public async Task ExportReadingsList_Returns524kItems()
    {
        var response = await _client.GetAsync("/api/export-readings-list");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<double>>();
        Assert.NotNull(list);
        Assert.Equal(524288, list.Count);
    }

    [Fact]
    public async Task ExportReadingsPooled_Returns524kItems()
    {
        var response = await _client.GetAsync("/api/export-readings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<double>>();
        Assert.NotNull(list);
        Assert.Equal(524288, list.Count);
    }

    [Fact]
    public async Task ExportReadingsStream_Returns524kItems()
    {
        var response = await _client.GetAsync("/api/export-readings-stream");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<double>>();
        Assert.NotNull(list);
        Assert.Equal(524288, list.Count);
    }

    // --- 2. 原生字串 string (50,000 筆) ---
    [Fact]
    public async Task ExportStringsList_Returns50kStrings()
    {
        var response = await _client.GetAsync("/api/export-strings-list");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.NotNull(list);
        Assert.Equal(50000, list.Count);
    }

    [Fact]
    public async Task ExportStringsPooled_Returns50kStrings()
    {
        var response = await _client.GetAsync("/api/export-strings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.NotNull(list);
        Assert.Equal(50000, list.Count);
    }

    [Fact]
    public async Task ExportStringsStream_Returns50kStrings()
    {
        var response = await _client.GetAsync("/api/export-strings-stream");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.NotNull(list);
        Assert.Equal(50000, list.Count);
    }

    // --- 3. 巢狀結構 Struct (20,000 筆) ---
    [Fact]
    public async Task ExportMembersList_Returns20kMembers()
    {
        var response = await _client.GetAsync("/api/export-members-list");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<MemberAccount>>(Options);
        Assert.NotNull(list);
        Assert.Equal(20000, list.Count);
    }

    [Fact]
    public async Task ExportMembersPooled_Returns20kMembers()
    {
        var response = await _client.GetAsync("/api/export-members");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<MemberAccount>>(Options);
        Assert.NotNull(list);
        Assert.Equal(20000, list.Count);
    }

    [Fact]
    public async Task ExportMembersStream_Returns20kMembers()
    {
        var response = await _client.GetAsync("/api/export-members-stream");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<MemberAccount>>(Options);
        Assert.NotNull(list);
        Assert.Equal(20000, list.Count);
    }

    // --- 4. 參考型別 Class (20,000 筆) ---
    [Fact]
    public async Task ExportMembersClassList_Returns20kMembers()
    {
        var response = await _client.GetAsync("/api/export-members-class-list");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<MemberAccountClass>>(Options);
        Assert.NotNull(list);
        Assert.Equal(20000, list.Count);
    }

    [Fact]
    public async Task ExportMembersClassPooled_Returns20kMembers()
    {
        var response = await _client.GetAsync("/api/export-members-class-pooled");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<MemberAccountClass>>(Options);
        Assert.NotNull(list);
        Assert.Equal(20000, list.Count);
    }

    [Fact]
    public async Task ExportMembersClassStream_Returns20kMembers()
    {
        var response = await _client.GetAsync("/api/export-members-class-stream");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<MemberAccountClass>>(Options);
        Assert.NotNull(list);
        Assert.Equal(20000, list.Count);
    }
}
