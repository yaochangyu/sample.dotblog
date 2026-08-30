using System.Text.Json;
using System.Text.Json.Serialization;
using Lab.LargeObject.Api;
using Reqnroll;
using Xunit;

namespace Lab.LargeObject.Specs.StepDefinitions;

[Binding]
public class LargeObjectResponseSteps
{
    private readonly HttpClient _client;
    private int _receivedReadingsCount;
    private double _receivedReadingsSum;
    private int _receivedMembersCount;
    private int _receivedActiveMembersCount;
    private int _receivedStringsCount;
    private long _receivedStringsTotalLength;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public LargeObjectResponseSteps(HttpClient client)
    {
        _client = client;
    }

    [When(@"用戶端以串流方式發送 GET 請求至 ""(.*)""")]
    public async Task WhenClientSendsStreamingGetRequest(string endpoint)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();

        if (endpoint.Contains("readings"))
        {
            await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<double>(stream, cancellationToken: default))
            {
                _receivedReadingsCount++;
                _receivedReadingsSum += item;
            }
        }
        else if (endpoint.Contains("strings"))
        {
            await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<string>(stream, cancellationToken: default))
            {
                if (item != null)
                {
                    _receivedStringsCount++;
                    _receivedStringsTotalLength += item.Length;
                }
            }
        }
        else if (endpoint.Contains("members"))
        {
            await foreach (var member in JsonSerializer.DeserializeAsyncEnumerable<MemberAccount>(stream, JsonOptions, cancellationToken: default))
            {
                _receivedMembersCount++;
                if (member.Status == MemberStatus.Active)
                {
                    _receivedActiveMembersCount++;
                }
            }
        }
    }

    [Then(@"用戶端應該成功接收 (.*) 筆數值")]
    public void ThenClientShouldReceiveReadingsCount(int expectedCount)
    {
        Assert.Equal(expectedCount, _receivedReadingsCount);
    }

    [Then(@"接收數值的累加總和應該大於 (.*)")]
    public void ThenReceivedReadingsSumShouldBeGreaterThan(double minSum)
    {
        Assert.True(_receivedReadingsSum > minSum);
    }

    [Then(@"用戶端應該成功接收 (.*) 筆會員資料")]
    public void ThenClientShouldReceiveMembersCount(int expectedCount)
    {
        Assert.Equal(expectedCount, _receivedMembersCount);
    }

    [Then(@"接收到的啟用會員數應該大於 (.*)")]
    public void ThenReceivedActiveMembersShouldBeGreaterThan(int minCount)
    {
        Assert.True(_receivedActiveMembersCount > minCount);
    }

    [Then(@"用戶端應該成功接收 (.*) 筆字串")]
    public void ThenClientShouldReceiveStringsCount(int expectedCount)
    {
        Assert.Equal(expectedCount, _receivedStringsCount);
    }

    [Then(@"接收到的字串總長度應該大於 (.*)")]
    public void ThenReceivedStringsLengthShouldBeGreaterThan(long minLength)
    {
        Assert.True(_receivedStringsTotalLength > minLength);
    }
}
