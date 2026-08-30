using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lab.LargeObject.Api;
using Reqnroll;
using Xunit;

namespace Lab.LargeObject.Specs.StepDefinitions;

[Binding]
public class LargeObjectRequestSteps
{
    private readonly HttpClient _client;
    private double[]? _doubleArray;
    private List<MemberAccount>? _memberList;
    private string[]? _stringArray;
    private HttpResponseMessage? _response;
    private ReadingsSummary? _readingsSummary;
    private MemberAccountSummary? _memberSummary;
    private StringsSummary? _stringSummary;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public LargeObjectRequestSteps(HttpClient client)
    {
        _client = client;
    }

    [Given(@"準備了 (.*) 筆 double 數值陣列")]
    public void GivenPreparedDoubleArray(int count)
    {
        _doubleArray = new double[count];
        for (var i = 0; i < count; i++)
        {
            _doubleArray[i] = i + 0.5;
        }
    }

    [Given(@"準備了 (.*) 筆會員帳號資料")]
    public void GivenPreparedMemberList(int count)
    {
        _memberList = new List<MemberAccount>(count);
        for (var i = 0; i < count; i++)
        {
            var status = (i % 3) switch
            {
                0 => MemberStatus.Active,
                1 => MemberStatus.Suspended,
                _ => MemberStatus.Deleted
            };
            _memberList.Add(new MemberAccount
            {
                MemberId = i,
                Account = $"member{i:D6}",
                DisplayName = $"會員 {i}",
                Status = status,
                Contact = new ContactInfo
                {
                    Email = $"member{i:D6}@example.com",
                    PhoneNumber = (i % 2 == 0) ? $"09{i:D8}" : null
                },
                CreatedAt = new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc)
            });
        }
    }

    [Given(@"準備了 (.*) 筆字串陣列")]
    public void GivenPreparedStringArray(int count)
    {
        _stringArray = new string[count];
        for (var i = 0; i < count; i++)
        {
            _stringArray[i] = $"sample-log-record-uuid-0000-0000-000000000000-{i:D8}-dotblog-benchmark";
        }
    }

    [When(@"發送 POST 請求至 ""(.*)""")]
    public async Task WhenSendPostRequest(string endpoint)
    {
        if (_doubleArray != null)
        {
            _response = await _client.PostAsJsonAsync(endpoint, _doubleArray);
            if (_response.IsSuccessStatusCode)
            {
                _readingsSummary = await _response.Content.ReadFromJsonAsync<ReadingsSummary>(JsonOptions);
            }
        }
        else if (_stringArray != null)
        {
            _response = await _client.PostAsJsonAsync(endpoint, _stringArray);
            if (_response.IsSuccessStatusCode)
            {
                _stringSummary = await _response.Content.ReadFromJsonAsync<StringsSummary>(JsonOptions);
            }
        }
    }

    [When(@"以串流方式發送 POST 請求至 ""(.*)""")]
    public async Task WhenSendStreamPostRequest(string endpoint)
    {
        var json = JsonSerializer.Serialize(_memberList, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        _response = await _client.PostAsync(endpoint, content);
        if (_response.IsSuccessStatusCode)
        {
            _memberSummary = await _response.Content.ReadFromJsonAsync<MemberAccountSummary>(JsonOptions);
        }
    }

    [Then(@"API 回傳狀態碼 (.*)")]
    public void ThenApiResponseStatusCode(int statusCode)
    {
        Assert.NotNull(_response);
        Assert.Equal((HttpStatusCode)statusCode, _response.StatusCode);
    }

    [Then(@"回傳的總筆數應該為 (.*)")]
    public void ThenReadingsCountShouldBe(int count)
    {
        Assert.NotNull(_readingsSummary);
        Assert.Equal(count, _readingsSummary.Count);
    }

    [Then(@"回傳的總和應該大於 (.*)")]
    public void ThenReadingsSumShouldBeGreaterThan(double minSum)
    {
        Assert.NotNull(_readingsSummary);
        Assert.True(_readingsSummary.Sum > minSum);
    }

    [Then(@"回傳的會員總數應該為 (.*)")]
    public void ThenMembersCountShouldBe(int count)
    {
        Assert.NotNull(_memberSummary);
        Assert.Equal(count, _memberSummary.Count);
    }

    [Then(@"啟用中的會員數應該大於 (.*)")]
    public void ThenActiveMembersCountShouldBeGreaterThan(int minCount)
    {
        Assert.NotNull(_memberSummary);
        Assert.True(_memberSummary.ActiveCount > minCount);
    }

    [Then(@"回傳的字串總筆數應該為 (.*)")]
    public void ThenStringsCountShouldBe(int count)
    {
        Assert.NotNull(_stringSummary);
        Assert.Equal(count, _stringSummary.Count);
    }

    [Then(@"回傳的總長度應該大於 (.*)")]
    public void ThenStringsTotalLengthShouldBeGreaterThan(long minLength)
    {
        Assert.NotNull(_stringSummary);
        Assert.True(_stringSummary.TotalLength > minLength);
    }
}
