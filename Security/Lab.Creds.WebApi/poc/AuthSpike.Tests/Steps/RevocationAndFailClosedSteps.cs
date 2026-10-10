using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthSpike.Hosting;
using AuthSpike.Signing;
using AuthSpike.Tests.Support;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>07 單：管理者撤銷、60 秒內阻擋、既有連線與多執行個體、查證故障 fail closed 的實際呼叫情境。</summary>
[Binding]
public sealed class RevocationAndFailClosedSteps : IDisposable
{
    private const string ClientId = "orders-client";
    /// <summary>每次取用都產生新的業務識別（orderReference），使各 Scenario 建立的訂單互不去重（06 單）。</summary>
    private static string OrderJson() => $$"""{"orderReference":"{{Guid.NewGuid():N}}","item":"book","quantity":1}""";
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/07-revocation-and-fail-closed.md";
    private const string OpenApiRelativePath = "poc/AuthSpike/doc/openapi.yml";

    /// <summary>撤銷必須在此時間內被各驗證端拒絕（07 單門檻）。</summary>
    private static readonly TimeSpan RevocationThreshold = TimeSpan.FromSeconds(60);

    /// <summary>@short-cache 環境的 Token 查證快取上限（與 SpikeHooks 一致）。</summary>
    private static readonly TimeSpan ShortCacheLifetime = TimeSpan.FromSeconds(30);

    private readonly Dictionary<string, Guid> _orders = new();
    private HttpClient? _client;
    private string? _token;
    private Guid _currentOrderId;
    private DateTimeOffset? _cachePopulatedAt;
    private DateTimeOffset? _revokedAt;
    private DateTimeOffset? _firstRejectAt;
    private DateTimeOffset? _secondaryRejectAt;
    private int? _portBeforeRevoke;
    private int? _portAfterRevoke;
    private HttpStatusCode? _lastStatus;
    private string _lastBody = string.Empty;
    private readonly List<(HttpStatusCode Status, string Body)> _queryResults = [];

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    [Given("07 單的實作紀錄可讀取")]
    public void GivenIssueRecordReadable()
    {
        File.Exists(Path.Combine(RepoRoot, IssueRelativePath)).Should().BeTrue();
    }

    [Given("簽章呼叫端 {string} 以 mTLS 取得 Token")]
    public async Task GivenCallerHasToken(string clientId)
    {
        _token = await RequestTokenAsync();
    }

    [Given("簽章呼叫端 {string} 以 mTLS 取得 Token 並成功建立訂單 {string}")]
    public async Task GivenCallerCreatesOrder(string clientId, string orderName)
    {
        _token = await RequestTokenAsync();
        await CreateOrderAsync(orderName);
        _cachePopulatedAt = DateTimeOffset.UtcNow;
    }

    [Given("簽章呼叫端 {string} 以既有連線取得 Token 並成功建立訂單 {string}")]
    public async Task GivenCallerCreatesOrderOnExistingConnection(string clientId, string orderName)
    {
        _token = await RequestTokenAsync();
        await CreateOrderAsync(orderName);
        _portBeforeRevoke = Runtime.OrdersApi.LastRemotePort;
    }

    [Given("簽章呼叫端 {string} 的查詢請求回應 200")]
    public async Task GivenQueryReturnsOk(string clientId)
    {
        var (status, body) = await QueryAsync(Runtime.OrdersApi.Port);
        status.Should().Be(HttpStatusCode.OK, body);
    }

    [Given("簽章呼叫端 {string} 的查詢請求於第二個執行個體回應 200")]
    public async Task GivenQueryOnSecondaryReturnsOk(string clientId)
    {
        var (status, body) = await QueryAsync(Runtime.SecondaryOrdersApi!.Port);
        status.Should().Be(HttpStatusCode.OK, body);
    }

    [Given("授權伺服器停止服務")]
    public async Task GivenAuthServerStopped()
    {
        await Runtime.AuthServer.StopAsync();
    }

    [When("管理者撤銷 {string}")]
    public async Task WhenAdministratorRevokes(string target)
    {
        _revokedAt = DateTimeOffset.UtcNow;
        switch (target)
        {
            case "Token":
                await Runtime.AuthServer.RevokeAccessTokenAsync(_token!);
                break;
            case "Client":
                Runtime.Registry.DisableClient(ClientId);
                break;
            case "mTLS 憑證":
                Runtime.Registry.RevokeCertificate(Runtime.ClientCertificate(ClientId).Thumbprint);
                break;
            case "簽章金鑰":
                Runtime.Registry.RevokeSigningKey(Runtime.SigningKey(ClientId).KeyId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(target), target, "未知的撤銷對象");
        }
    }

    [When("在快取有效期限內送出查詢請求")]
    public async Task WhenQueryWithinCacheLifetime()
    {
        (DateTimeOffset.UtcNow < _cachePopulatedAt!.Value.Add(ShortCacheLifetime))
            .Should().BeTrue("測試時間已超出查證快取期限，無法驗證快取內行為");
        var (status, body) = await QueryAsync(Runtime.OrdersApi.Port);
        _lastStatus = status;
        _lastBody = body;
    }

    [When("等待查證快取過期後連續送出 {int} 次查詢請求")]
    public async Task WhenQueryAfterCacheExpiry(int count)
    {
        var wait = _cachePopulatedAt!.Value.Add(ShortCacheLifetime).AddMilliseconds(500) - DateTimeOffset.UtcNow;
        if (wait > TimeSpan.Zero)
        {
            await Task.Delay(wait);
        }

        _queryResults.Clear();
        for (var index = 0; index < count; index++)
        {
            _queryResults.Add(await QueryAsync(Runtime.OrdersApi.Port));
        }
    }

    [When("授權伺服器停止後，簽章呼叫端 {string} 以 Idempotency-Key {string} 送出建立訂單請求")]
    public async Task WhenOrderSentAfterAuthServerStopped(string clientId, string idempotencyKey)
    {
        var template = await BuildSignedOrderAsync(idempotencyKey);
        (_lastStatus, _lastBody) = await SendAsync(template, Runtime.OrdersApi.Port);
    }

    [When("簽章呼叫端 {string} 以 Idempotency-Key {string} 送出建立訂單請求")]
    public async Task WhenOrderSentWithIdempotencyKey(string clientId, string idempotencyKey)
    {
        var template = await BuildSignedOrderAsync(idempotencyKey);
        (_lastStatus, _lastBody) = await SendAsync(template, Runtime.OrdersApi.Port);
    }

    [Then("撤銷後的查詢請求於 60 秒內回應 401")]
    public async Task ThenRevokedQueryRejectedWithinThreshold()
    {
        var rejectedAt = await PollUntilRejectedAsync(Runtime.OrdersApi.Port, first: true);
        _firstRejectAt = rejectedAt;
        Measure("主要執行個體", rejectedAt);
    }

    [Then("第二個執行個體的撤銷後查詢於 60 秒內回應 401")]
    public async Task ThenSecondaryRevokedQueryRejectedWithinThreshold()
    {
        var rejectedAt = await PollUntilRejectedAsync(Runtime.SecondaryOrdersApi!.Port, first: false);
        _secondaryRejectAt = rejectedAt;
        Measure("第二個執行個體", rejectedAt);
    }

    [Then("實際測得的撤銷延遲小於 60 秒")]
    public void ThenMeasuredRevocationDelayBelowThreshold()
    {
        _revokedAt.Should().NotBeNull();
        _firstRejectAt.Should().NotBeNull();
        (_firstRejectAt!.Value - _revokedAt!.Value).Should().BeLessThan(RevocationThreshold);
    }

    [Then("後續查詢請求使用的連線與撤銷前相同")]
    public void ThenFollowUpUsesSameConnection()
    {
        _portAfterRevoke.Should().NotBeNull();
        _portAfterRevoke.Should().Be(_portBeforeRevoke);
    }

    [Then("授權伺服器對既有 Token 的 introspection 仍為 active")]
    public async Task ThenIntrospectionStillActive()
    {
        using var client = CreateClient(Runtime.ClientCertificate(SpikeRuntime.ResourceClientId));
        using var response = await client.PostAsync(
            new Uri(Runtime.AuthServer.Issuer, "connect/introspect"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["token"] = _token!,
                ["token_type_hint"] = "access_token",
                ["client_id"] = SpikeRuntime.ResourceClientId,
            }));

        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);
        using var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("active").GetBoolean().Should().BeTrue("撤銷 Client、憑證或金鑰不應使既有 Token 被自動視為失效");
    }

    [Then("被快取允許的查詢回應為 200")]
    public void ThenCachedQueryAllowed()
    {
        _lastStatus.Should().Be(HttpStatusCode.OK, _lastBody);
    }

    [Then("所有查詢回應為 503 且錯誤代碼為 {string}")]
    public void ThenAllQueriesUnavailable(string code)
    {
        _queryResults.Should().HaveCount(2);
        foreach (var (status, body) in _queryResults)
        {
            status.Should().Be(HttpStatusCode.ServiceUnavailable, body);
            ErrorCodeOf(body).Should().Be(code);
        }
    }

    [Then("查詢回應不是憑證無效的 401")]
    public void ThenQueriesNotDisguisedAs401()
    {
        _queryResults.Should().OnlyContain(result => result.Status != HttpStatusCode.Unauthorized);
    }

    [Then("最後一次嘗試的回應為 {int} 且錯誤代碼為 {string}")]
    public void ThenLastAttemptReturnsWithCode(int statusCode, string code)
    {
        _lastStatus.Should().Be((HttpStatusCode)statusCode, _lastBody);
        ErrorCodeOf(_lastBody).Should().Be(code);
    }

    [Then("最後一次嘗試的回應為 {int}")]
    public void ThenLastAttemptReturns(int statusCode)
    {
        _lastStatus.Should().Be((HttpStatusCode)statusCode, _lastBody);
    }

    [Then("最後一次嘗試的回應不是憑證無效的 401")]
    public void ThenLastAttemptNotDisguisedAs401()
    {
        _lastStatus.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Then("重新以 mTLS 取得新 Token 後查詢 {string} 的狀態仍為 {string}")]
    public async Task ThenOrderStillHasStatusAfterRenewal(string orderName, string expectedStatus)
    {
        _token = await RequestTokenAsync();
        _currentOrderId = _orders[orderName];
        var (status, body) = await QueryAsync(Runtime.OrdersApi.Port);
        status.Should().Be(HttpStatusCode.OK, body);
        using var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("status").GetString().Should().Be(expectedStatus);
    }

    [Then("07 單第 {int} 項驗收已勾選")]
    public void ThenIssueCheckboxIsChecked(int index)
    {
        CheckboxAt(index).Should().StartWith("- [x] ");
    }

    [Then("07 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    [Then("07 單的 API 契約包含 {string}")]
    public void ThenApiContractContains(string expected)
    {
        File.ReadAllText(Path.Combine(RepoRoot, OpenApiRelativePath)).Should().Contain(expected);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    private void Measure(string endpoint, DateTimeOffset rejectedAt)
    {
        var elapsed = rejectedAt - _revokedAt!.Value;
        Console.WriteLine($"[07 測量] {endpoint}：撤銷起點 {_revokedAt:O}，首次拒絕 {rejectedAt:O}，耗時 {elapsed.TotalSeconds:F2} 秒");
        elapsed.Should().BeLessThanOrEqualTo(RevocationThreshold);
    }

    private async Task<DateTimeOffset> PollUntilRejectedAsync(int port, bool first)
    {
        var deadline = _revokedAt!.Value.Add(RevocationThreshold);
        while (true)
        {
            var template = await BuildSignedQueryTemplateAsync(_currentOrderId);
            var (status, body) = await SendAsync(template, port);
            if (first && _portAfterRevoke is null)
            {
                _portAfterRevoke = Runtime.OrdersApi.LastRemotePort;
            }

            var now = DateTimeOffset.UtcNow;
            if (status == HttpStatusCode.Unauthorized)
            {
                return now;
            }

            if (now > deadline)
            {
                throw new InvalidOperationException($"撤銷後超過 60 秒仍未被拒絕，最後回應 {(int)status}：{body}");
            }

            await Task.Delay(250);
        }
    }

    private async Task CreateOrderAsync(string orderName)
    {
        var template = await BuildSignedOrderAsync(Guid.NewGuid().ToString());
        var (status, body) = await SendAsync(template, Runtime.OrdersApi.Port);
        status.Should().Be(HttpStatusCode.Created, body);
        using var document = JsonDocument.Parse(body);
        _currentOrderId = document.RootElement.GetProperty("orderId").GetGuid();
        _orders[orderName] = _currentOrderId;
    }

    private async Task<(HttpStatusCode Status, string Body)> QueryAsync(int port)
    {
        var template = await BuildSignedQueryTemplateAsync(_currentOrderId);
        return await SendAsync(template, port);
    }

    private async Task<HttpRequestMessage> BuildSignedQueryTemplateAsync(Guid orderId)
    {
        var now = DateTimeOffset.UtcNow;
        var request = new HttpRequestMessage(HttpMethod.Get, OrdersUri(Runtime.OrdersApi.Port, $"orders/{orderId}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        await BusinessRequestSigner.SignAsync(request, Runtime.SigningKey(ClientId), now, now.AddSeconds(60), NewNonce());
        return request;
    }

    private async Task<HttpRequestMessage> BuildSignedOrderAsync(string idempotencyKey)
    {
        var now = DateTimeOffset.UtcNow;
        var request = new HttpRequestMessage(HttpMethod.Post, OrdersUri(Runtime.OrdersApi.Port, "orders"))
        {
            Content = new StringContent(OrderJson(), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        await BusinessRequestSigner.SignAsync(request, Runtime.SigningKey(ClientId), now, now.AddSeconds(60), NewNonce());
        return request;
    }

    /// <summary>以持久連線送出；port 與主要執行個體不同時沿用主要執行個體的公開目標（Host），模擬經同一入口的多個執行個體。</summary>
    private async Task<(HttpStatusCode Status, string Body)> SendAsync(HttpRequestMessage template, int port)
    {
        using var request = await CloneAsync(template, port);
        using var response = await PersistentClient().SendAsync(request);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private HttpClient PersistentClient() => _client ??= CreateClient(Runtime.ClientCertificate(ClientId));

    private async Task<string> RequestTokenAsync()
    {
        using var response = await PersistentClient().PostAsync(
            new Uri(Runtime.AuthServer.Issuer, "connect/token"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = ClientId,
            }));

        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("access_token").GetString()!;
    }

    private static HttpClient CreateClient(X509Certificate2 certificate)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = Runtime.Trust.ServerCertificateValidator,
            ClientCertificateOptions = ClientCertificateOption.Manual,
        };
        handler.ClientCertificates.Add(certificate);
        return new HttpClient(handler, disposeHandler: true);
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage source, int port)
    {
        var primaryPort = Runtime.OrdersApi.Port;
        var clone = new HttpRequestMessage(source.Method, new UriBuilder(source.RequestUri!) { Port = port }.Uri);
        foreach (var header in source.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (port != primaryPort)
        {
            clone.Headers.Host = $"localhost:{primaryPort}";
        }

        if (source.Content is not null)
        {
            clone.Content = new ByteArrayContent(await source.Content.ReadAsByteArrayAsync());
            foreach (var header in source.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }

    private static string ErrorCodeOf(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("error").GetString()!;
    }

    private static string NewNonce() => Guid.NewGuid().ToString("N");

    private static Uri OrdersUri(int port, string path) => new($"https://localhost:{port}/{path}");


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
