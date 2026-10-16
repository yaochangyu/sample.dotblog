using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthSpike.Audit;
using AuthSpike.Hosting;
using AuthSpike.Signing;
using AuthSpike.Tests.Support;
using AuthSpike.Verification;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>03 單：呼叫者類型與 audience 隔離。全部經真實 TLS、真實 client_credentials／password grant、內省與建立訂單 API。</summary>
[Binding]
public sealed class CallerTypeAndAudienceIsolationSteps
{
    private const string IssueRelativePath = ".scratch/dual-caller-authentication/issues/03-caller-type-and-audience-isolation.md";
    private const string ClientIdHeader = "X-Client-Id";
    private const string CallerTypeHeader = "X-Caller-Type";

    private string? _serviceToken;
    private string? _userToken;
    private HttpStatusCode? _status;
    private string _responseBody = string.Empty;
    private string? _correlationId;
    private string _introspectionBody = string.Empty;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    private static Uri OrdersUri => new($"https://localhost:{Runtime.OrdersApi.Port}/orders");

    [Given("持有服務 {string} 的服務 Token（03 單）")]
    public async Task GivenServiceToken(string clientId)
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
        _serviceToken = JsonDocument.Parse(body).RootElement.GetProperty("access_token").GetString();
        _serviceToken.Should().NotBeNullOrEmpty();
    }

    [Given("持有使用者 Token（03 單）")]
    public async Task GivenUserToken()
    {
        using var client = SignedHttp.CreateClient(Runtime, certificate: null);
        using var response = await client.PostAsync(
            new Uri(Runtime.AuthServer.Issuer, "connect/token"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = UserCallers.ClientId,
                ["username"] = UserCallers.TestUserId,
                ["password"] = UserCallers.TestUserPassword,
            }));
        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);
        _userToken = JsonDocument.Parse(body).RootElement.GetProperty("access_token").GetString();
        _userToken.Should().NotBeNullOrEmpty();
    }

    [When("持服務 Token 的呼叫端不附用戶端憑證，送出建立訂單請求（03 單）")]
    public async Task WhenServiceTokenWithoutCertificate()
    {
        await SendAsync(OrderRequest(_serviceToken, OrderBody(), []), certificate: null);
    }

    [When("持服務 Token 的呼叫端不附用戶端憑證，送出建立訂單請求，並附 X-Client-Id 標頭 {string}（03 單）")]
    public async Task WhenServiceTokenWithoutCertificateClaimsClient(string claimedClientId)
    {
        await SendAsync(OrderRequest(_serviceToken, OrderBody(), [new(ClientIdHeader, claimedClientId)]), certificate: null);
    }

    [When("使用者呼叫端不附用戶端憑證，送出建立訂單請求（03 單）")]
    public async Task WhenUserCallerCreatesOrder()
    {
        await SendAsync(OrderRequest(_userToken, OrderBody(), []), certificate: null);
    }

    [When("使用者呼叫端不附用戶端憑證，送出建立訂單請求，Body 與標頭宣稱為服務 {string}（03 單）")]
    public async Task WhenUserCallerClaimsServiceIdentity(string claimedClientId)
    {
        var body = OrderBody(new Dictionary<string, object?>
        {
            ["callerType"] = "service",
            ["clientId"] = claimedClientId,
        });
        await SendAsync(
            OrderRequest(_userToken, body, [new(CallerTypeHeader, "service"), new(ClientIdHeader, claimedClientId)]),
            certificate: null);
    }

    [When("服務 {string} 以憑證持使用者 Token 送出已簽章的建立訂單請求（03 單）")]
    public async Task WhenServiceWithCertificateHoldsUserToken(string clientId)
    {
        _userToken.Should().NotBeNullOrEmpty();
        var request = await SignedHttp.BuildSignedAsync(HttpMethod.Post, OrdersUri, _userToken, Runtime.SigningKey(clientId), OrderBody(), Guid.NewGuid().ToString());
        await SendAsync(request, Runtime.ClientCertificate(clientId));
    }

    [When("服務 {string} 以憑證持服務 Token 送出未簽章的建立訂單請求（03 單）")]
    public async Task WhenServiceWithCertificateSendsUnsignedOrder(string clientId)
    {
        await SendAsync(OrderRequest(_serviceToken, OrderBody(), []), Runtime.ClientCertificate(clientId));
    }

    [When("服務 {string} 以憑證持服務 Token 送出已簽章的建立訂單請求（03 單）")]
    public async Task WhenServiceWithCertificateSendsSignedOrder(string clientId)
    {
        var request = await SignedHttp.BuildSignedAsync(HttpMethod.Post, OrdersUri, _serviceToken, Runtime.SigningKey(clientId), OrderBody(), Guid.NewGuid().ToString());
        await SendAsync(request, Runtime.ClientCertificate(clientId));
    }

    [When("以使用者查證身分內省服務 Token（03 單）")]
    public async Task WhenIntrospectServiceTokenAsUser()
    {
        await IntrospectAsync(_serviceToken, UserCallers.IntrospectionClientId);
    }

    [When("以業務 API 服務身分內省使用者 Token（03 單）")]
    public async Task WhenIntrospectUserTokenAsResource()
    {
        await IntrospectAsync(_userToken, SpikeRuntime.ResourceClientId);
    }

    [Then("03 單呼叫端最近一次請求回應 {int}")]
    public void ThenLastRequestReturns(int statusCode)
    {
        _status.Should().Be((HttpStatusCode)statusCode, _responseBody);
    }

    [Then("03 單建立訂單總數為 {int}")]
    public void ThenOrderCountIs(int count)
    {
        Runtime.Orders.Count.Should().Be(count);
    }

    [Then("03 單回應 clientId 為 {string}")]
    public void ThenResponseClientIdIs(string expectedClientId)
    {
        _status.Should().Be(HttpStatusCode.Created, _responseBody);
        using var document = JsonDocument.Parse(_responseBody);
        document.RootElement.GetProperty("clientId").GetString().Should().Be(expectedClientId);
    }

    [Then("03 單內省結果為 inactive")]
    public void ThenIntrospectionIsInactive()
    {
        using var document = JsonDocument.Parse(_introspectionBody);
        document.RootElement.GetProperty("active").GetBoolean().Should().BeFalse(_introspectionBody);
    }

    [Then("03 單最近一次稽核紀錄結果為 {string}")]
    public void ThenLastAuditOutcomeIs(string outcome)
    {
        LastRecord().Outcome.Should().Be(outcome);
    }

    [Then("03 單最近一次稽核紀錄沒有已驗證 Client")]
    public void ThenLastAuditHasNoVerifiedClient()
    {
        LastRecord().VerifiedClientId.Should().BeNull();
    }

    [Then("呼叫者隔離票第 {int} 項驗收已勾選")]
    public void ThenIssueCheckboxIsChecked(int index)
    {
        CheckboxAt(index).Should().StartWith("- [x] ");
    }

    [Then("呼叫者隔離票的狀態為 resolved")]
    public void ThenIssueStatusIsResolved()
    {
        IssueText.Should().Contain("**Status:** resolved");
    }

    private async Task SendAsync(HttpRequestMessage request, X509Certificate2? certificate)
    {
        using var client = SignedHttp.CreateClient(Runtime, certificate);
        using var response = await client.SendAsync(request);
        _status = response.StatusCode;
        _responseBody = await response.Content.ReadAsStringAsync();
        _correlationId = response.Headers.TryGetValues("X-Correlation-Id", out var values) ? values.Single() : null;
    }

    private async Task IntrospectAsync(string? token, string introspectingClientId)
    {
        token.Should().NotBeNullOrEmpty();
        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(SpikeRuntime.ResourceClientId));
        using var response = await client.PostAsync(
            new Uri(Runtime.AuthServer.Issuer, "connect/introspect"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["token"] = token!,
                ["token_type_hint"] = "access_token",
                ["client_id"] = introspectingClientId,
            }));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _introspectionBody = await response.Content.ReadAsStringAsync();
    }

    private static HttpRequestMessage OrderRequest(string? bearer, string body, IEnumerable<KeyValuePair<string, string>> headers)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, OrdersUri)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString());

        // 使用者寫入需附防重放 nonce（05 單）；服務路徑不讀取此標頭，簽章也不涵蓋它。
        request.Headers.TryAddWithoutValidation("X-Request-Nonce", SignedHttp.NewNonce());
        foreach (var (name, value) in headers)
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        return request;
    }

    /// <summary>每次取用都產生新的業務識別（orderReference），使各 Scenario 建立的訂單互不去重。</summary>
    private static string OrderBody(IDictionary<string, object?>? extra = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["orderReference"] = SignedHttp.NewNonce(),
            ["item"] = "book",
            ["quantity"] = 1,
        };
        if (extra is not null)
        {
            foreach (var (key, value) in extra)
            {
                payload[key] = value;
            }
        }

        return JsonSerializer.Serialize(payload);
    }

    private SecurityAuditRecord LastRecord()
    {
        _correlationId.Should().NotBeNullOrEmpty("最近一次回應應帶 X-Correlation-Id");
        var records = Runtime.AuditLog.Snapshot().Where(record => record.CorrelationId == _correlationId).ToList();
        records.Should().ContainSingle("每筆請求應恰有一筆稽核紀錄");
        return records[0];
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
