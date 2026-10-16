using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthSpike.Hosting;
using AuthSpike.Tests.Support;
using AuthSpike.Verification;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>06 單：使用者 Token 撤銷與使用者停用（AC-D11）、查證故障 fail closed（AC-D12）。全部經真實 TLS 與真實內省。</summary>
[Binding]
public sealed class UserTokenRevocationAndFailClosedSteps : IDisposable
{
    private const string IssueRelativePath = ".scratch/dual-caller-authentication/issues/06-user-token-revocation-and-fail-closed.md";

    /// <summary>撤銷或停用必須在此時間內被拒絕（與 07 單相同的 60 秒上限，lab 暫定、待使用者確認）。</summary>
    private static readonly TimeSpan RevocationThreshold = TimeSpan.FromSeconds(60);

    /// <summary>@short-cache 環境的 Token 查證快取上限（與 SpikeHooks 一致）。</summary>
    private static readonly TimeSpan ShortCacheLifetime = TimeSpan.FromSeconds(4);

    /// <summary>每次取用都產生新的業務識別（orderReference），使各 Scenario 建立的訂單互不去重。</summary>
    private static string OrderJson() => $$"""{"orderReference":"{{Guid.NewGuid():N}}","item":"book","quantity":1}""";

    private const string ServiceClientId = "orders-client";

    private readonly Dictionary<string, Guid> _orders = new();
    private readonly Dictionary<string, string> _userTokens = new();
    private readonly Dictionary<string, string> _serviceTokens = new();
    private readonly List<(HttpStatusCode Status, string Body)> _queryResults = [];
    private string? _currentUserToken;
    private Guid _currentOrderId;
    private DateTimeOffset? _cachePopulatedAt;
    private DateTimeOffset? _revokedAt;
    private DateTimeOffset? _rejectedAt;
    private TimeSpan _cachedQueryElapsed;
    private HttpStatusCode? _lastStatus;
    private string _lastBody = string.Empty;
    private HttpClient? _userClient;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    private static Uri OrdersUri => new($"https://localhost:{Runtime.OrdersApi.Port}/orders");

    [Given("撤銷票使用者 {string} 取得使用者 Token")]
    public async Task GivenUserObtainsToken(string username)
    {
        _currentUserToken = await RequestUserTokenAsync(username);
    }

    [Given("撤銷票使用者 {string} 取得使用者 Token 並成功建立訂單 {string}")]
    public async Task GivenUserObtainsTokenAndCreatesOrder(string username, string orderName)
    {
        _currentUserToken = await RequestUserTokenAsync(username);
        await CreateUserOrderAsync(orderName);
        _cachePopulatedAt = DateTimeOffset.UtcNow;
    }

    [Given("撤銷票服務 {string} 以憑證取得服務 Token 並建立訂單 {string}")]
    public async Task GivenServiceObtainsTokenAndCreatesOrder(string clientId, string orderName)
    {
        _serviceTokens[clientId] = await RequestServiceTokenAsync(clientId);
        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(clientId));
        var request = await SignedHttp.BuildSignedAsync(HttpMethod.Post, OrdersUri, _serviceTokens[clientId], Runtime.SigningKey(clientId), OrderJson(), Guid.NewGuid().ToString());
        var response = await SignedHttp.SendAsync(client, request);
        RememberLast(response);
        response.Status.Should().Be(HttpStatusCode.Created, response.Body);
        _orders[orderName] = ReadOrderId(response.Body);
    }

    [Given("撤銷票授權伺服器停止服務")]
    public async Task GivenAuthServerStopped()
    {
        await Runtime.AuthServer.StopAsync();
    }

    [When("撤銷票管理者撤銷使用者 Token")]
    public async Task WhenAdministratorRevokesUserToken()
    {
        _currentUserToken.Should().NotBeNullOrEmpty();
        _revokedAt = DateTimeOffset.UtcNow;
        await Runtime.AuthServer.RevokeAccessTokenAsync(_currentUserToken!);
    }

    [When("撤銷票測試使用者 {string} 被停用")]
    public void WhenTestUserIsDisabled(string username)
    {
        _revokedAt = DateTimeOffset.UtcNow;
        Runtime.Registry.DisableUser(username);
    }

    [When("撤銷票使用者以使用者 Token 送出建立訂單請求")]
    public async Task WhenUserSendsCreateOrder()
    {
        _currentUserToken.Should().NotBeNullOrEmpty();
        await CreateUserOrderRequestAsync(_currentUserToken!);
    }

    [When("撤銷票在快取有效期限內以使用者 Token 查詢訂單 {string}")]
    public async Task WhenUserQueriesWithinCacheLifetime(string orderName)
    {
        (DateTimeOffset.UtcNow < _cachePopulatedAt!.Value.Add(ShortCacheLifetime))
            .Should().BeTrue("測試時間已超出查證快取期限，無法驗證快取內行為");
        _currentOrderId = _orders[orderName];
        var stopwatch = Stopwatch.StartNew();
        var (status, body) = await UserGetAsync(_currentOrderId);
        _cachedQueryElapsed = stopwatch.Elapsed;
        RememberLast((status, body));
    }

    [When("撤銷票等待使用者查證快取過期後連續送出 {int} 次使用者查詢")]
    public async Task WhenUserQueriesAfterCacheExpiry(int count)
    {
        var wait = _cachePopulatedAt!.Value.Add(ShortCacheLifetime).AddMilliseconds(500) - DateTimeOffset.UtcNow;
        if (wait > TimeSpan.Zero)
        {
            await Task.Delay(wait);
        }

        _queryResults.Clear();
        for (var index = 0; index < count; index++)
        {
            _queryResults.Add(await UserGetAsync(_currentOrderId));
        }
    }

    [When("撤銷票服務 {string} 以已簽章請求查詢訂單 {string}")]
    public async Task WhenServiceQueriesSigned(string clientId, string orderName)
    {
        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(clientId));
        var request = await SignedHttp.BuildSignedAsync(HttpMethod.Get, new Uri($"{OrdersUri}/{_orders[orderName]}"), _serviceTokens[clientId], Runtime.SigningKey(clientId));
        RememberLast(await SignedHttp.SendAsync(client, request));
    }

    [Then("撤銷票使用者查詢於 60 秒內回應 401")]
    public async Task ThenUserQueryRejectedWithinThreshold()
    {
        _revokedAt.Should().NotBeNull("必須先執行撤銷或停用");
        var deadline = _revokedAt!.Value.Add(RevocationThreshold);
        while (true)
        {
            var (status, body) = await UserGetAsync(_currentOrderId);
            var now = DateTimeOffset.UtcNow;
            if (status == HttpStatusCode.Unauthorized)
            {
                _rejectedAt = now;
                RememberLast((status, body));
                return;
            }

            if (now > deadline)
            {
                throw new InvalidOperationException($"撤銷或停用後超過 60 秒仍未被拒絕，最後回應 {(int)status}：{body}");
            }

            await Task.Delay(250);
        }
    }

    [Then("撤銷票實測的撤銷生效延遲小於 60 秒")]
    public void ThenMeasuredDelayBelowThreshold()
    {
        _revokedAt.Should().NotBeNull();
        _rejectedAt.Should().NotBeNull();
        var elapsed = _rejectedAt!.Value - _revokedAt!.Value;
        Console.WriteLine($"[06 單撤銷生效延遲] 撤銷起點 {_revokedAt:O}，首次拒絕 {_rejectedAt:O}，耗時 {elapsed.TotalSeconds:F2} 秒");
        elapsed.Should().BeLessThan(RevocationThreshold);
    }

    [Then("撤銷票最近一次請求回應 {int}")]
    public void ThenLastResponseIs(int statusCode)
    {
        _lastStatus.Should().Be((HttpStatusCode)statusCode, _lastBody);
    }

    [Then("撤銷票最近一次請求回應 {int} 且錯誤代碼為 {string}")]
    public void ThenLastResponseIsWithCode(int statusCode, string code)
    {
        _lastStatus.Should().Be((HttpStatusCode)statusCode, _lastBody);
        ErrorCodeOf(_lastBody).Should().Be(code);
    }

    [Then("撤銷票最近一次回應不是 Token 無效的 401")]
    public void ThenLastResponseNotDisguisedAs401()
    {
        _lastStatus.Should().NotBe(HttpStatusCode.Unauthorized, _lastBody);
    }

    [Then("撤銷票快取命中的查詢在 {int} 秒內完成")]
    public void ThenCachedQueryCompletedQuickly(int seconds)
    {
        _cachedQueryElapsed.Should().BeLessThan(TimeSpan.FromSeconds(seconds), "快取命中不應向已停止的授權伺服器 introspection");
    }

    [Then("撤銷票所有使用者查詢回應為 {int} 且錯誤代碼為 {string}")]
    public void ThenAllUserQueriesUnavailable(int statusCode, string code)
    {
        _queryResults.Should().HaveCount(2);
        foreach (var (status, body) in _queryResults)
        {
            status.Should().Be((HttpStatusCode)statusCode, body);
            ErrorCodeOf(body).Should().Be(code);
        }
    }

    [Then("撤銷票使用者查詢回應不是 Token 無效的 401")]
    public void ThenUserQueriesNotDisguisedAs401()
    {
        _queryResults.Should().OnlyContain(result => result.Status != HttpStatusCode.Unauthorized);
    }

    [Then("撤銷票業務 API 建立訂單總數為 {int}")]
    public void ThenOrderCountIs(int count)
    {
        Runtime.Orders.Count.Should().Be(count);
    }

    [Then("撤銷票第 {int} 項驗收已勾選")]
    public void ThenIssueCheckboxIsChecked(int index)
    {
        CheckboxAt(index).Should().StartWith("- [x] ");
    }


    [Then("撤銷票的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    public void Dispose()
    {
        _userClient?.Dispose();
    }

    private async Task<string> RequestUserTokenAsync(string username)
    {
        using var client = SignedHttp.CreateClient(Runtime, certificate: null);
        using var response = await client.PostAsync(
            new Uri(Runtime.AuthServer.Issuer, "connect/token"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = UserCallers.ClientId,
                ["username"] = username,
                ["password"] = UserCallers.TestUsers[username],
            }));
        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);
        return JsonDocument.Parse(body).RootElement.GetProperty("access_token").GetString()!;
    }

    private async Task<string> RequestServiceTokenAsync(string clientId)
    {
        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(clientId));
        using var response = await client.PostAsync(
            new Uri(Runtime.AuthServer.Issuer, "connect/token"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
            }));
        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);
        return JsonDocument.Parse(body).RootElement.GetProperty("access_token").GetString()!;
    }

    private async Task CreateUserOrderAsync(string orderName)
    {
        var response = await SendUserCreateOrderAsync(_currentUserToken!);
        RememberLast(response);
        response.Status.Should().Be(HttpStatusCode.Created, response.Body);
        _orders[orderName] = ReadOrderId(response.Body);
        _currentOrderId = _orders[orderName];
    }

    private async Task CreateUserOrderRequestAsync(string token)
    {
        RememberLast(await SendUserCreateOrderAsync(token));
    }

    /// <summary>使用者寫入沒有請求簽章，以每次不同的 X-Request-Nonce 防重放（05 單）；不附用戶端憑證。</summary>
    private static async Task<(HttpStatusCode Status, string Body)> SendUserCreateOrderAsync(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, OrdersUri)
        {
            Content = new StringContent(OrderJson(), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString());
        request.Headers.TryAddWithoutValidation("X-Request-Nonce", SignedHttp.NewNonce());
        using var client = SignedHttp.CreateClient(Runtime, certificate: null);
        using var response = await client.SendAsync(request);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    /// <summary>使用者讀取（04 單的 /orders/me 路徑）；讀取不需要防重放，也不附用戶端憑證。</summary>
    private async Task<(HttpStatusCode Status, string Body)> UserGetAsync(Guid orderId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{OrdersUri}/me/{orderId}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentUserToken);
        _userClient ??= SignedHttp.CreateClient(Runtime, certificate: null);
        using var response = await _userClient.SendAsync(request);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private void RememberLast(ApiResponse response) => RememberLast((response.Status, response.Body));

    private void RememberLast((HttpStatusCode Status, string Body) response)
    {
        _lastStatus = response.Status;
        _lastBody = response.Body;
    }

    private static Guid ReadOrderId(string body)
        => JsonDocument.Parse(body).RootElement.GetProperty("orderId").GetGuid();

    private static string ErrorCodeOf(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("error").GetString()!;
    }

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".scratch")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("找不到專案根目錄（.scratch）。");
        }
    }

    private static string IssueText => File.ReadAllText(Path.Combine(RepoRoot, IssueRelativePath));

    private static string CheckboxAt(int index)
    {
        var checkboxes = IssueText.Split('\n')
            .Where(line => Regex.IsMatch(line, @"^- \[[ x]\] "))
            .ToList();
        checkboxes.Should().HaveCountGreaterThanOrEqualTo(index);
        return checkboxes[index - 1];
    }
}
