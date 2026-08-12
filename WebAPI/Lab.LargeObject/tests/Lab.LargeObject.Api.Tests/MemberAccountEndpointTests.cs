using System.Net.Http.Json;
using Lab.LargeObject.Api;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lab.LargeObject.Api.Tests;

public class MemberAccountEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MemberAccountEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_Members_接收巢狀強型別物件的大陣列_回傳正確狀態統計()
    {
        // Arrange：20000 筆會員帳號（每筆含巢狀 ContactInfo），陣列容器本身遠超過 85000 bytes 的 LOH 門檻。
        const int memberCount = 20_000;
        var members = new MemberAccount[memberCount];
        for (var i = 0; i < memberCount; i++)
        {
            members[i] = new MemberAccount
            {
                MemberId = i,
                Account = $"member{i:D6}",
                DisplayName = $"會員 {i}",
                Status = (MemberStatus)(i % 3),
                Contact = new ContactInfo
                {
                    Email = $"member{i:D6}@example.com",
                    PhoneNumber = i % 2 == 0 ? $"09{i:D8}" : null
                },
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        var expectedActive = members.Count(m => m.Status == MemberStatus.Active);
        var expectedSuspended = members.Count(m => m.Status == MemberStatus.Suspended);
        var expectedDeleted = members.Count(m => m.Status == MemberStatus.Deleted);

        // Act
        var response = await _client.PostAsJsonAsync("/api/members", members);

        // Assert
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<MemberAccountSummary>();

        Assert.NotNull(summary);
        Assert.Equal(memberCount, summary!.Count);
        Assert.Equal(expectedActive, summary.ActiveCount);
        Assert.Equal(expectedSuspended, summary.SuspendedCount);
        Assert.Equal(expectedDeleted, summary.DeletedCount);
    }

    [Fact]
    public async Task Post_Members_空陣列_回傳Count為0()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/members", Array.Empty<MemberAccount>());

        // Assert
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<MemberAccountSummary>();

        Assert.NotNull(summary);
        Assert.Equal(0, summary!.Count);
        Assert.Equal(0, summary.ActiveCount);
        Assert.Equal(0, summary.SuspendedCount);
        Assert.Equal(0, summary.DeletedCount);
    }
}
