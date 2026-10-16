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

/// <summary>08 單：同一建立訂單端點的雙呼叫者端到端驗收。全部經真實 TLS、真實 password grant、真實 client_credentials 與已簽章的服務請求。</summary>
[Binding]
public sealed class DualCallerEndToEndSteps
{
    private const string IssueRelativePath = ".scratch/dual-caller-authentication/issues/08-dual-caller-acceptance.md";
    private const string ServiceClientId = "orders-client";
    private const string DefaultItem = "book";

    private readonly Dictionary<string, string> _userTokens = new();
    private readonly Dictionary<string, Guid> _orderIds = new();
    private readonly Dictionary<string, string> _orderOwners = new();
    private (string Username, string Key, string Body)? _lastUserCreate;
    private string? _serviceToken;
    private HttpStatusCode? _lastStatus;
    private string _lastBody = string.Empty;
    private bool _lastReplayed;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    private static Uri OrdersUri => new($"https://localhost:{Runtime.OrdersApi.Port}/orders");

    [Given("雙呼叫者票使用者 {string} 取得使用者 Token")]
    public async Task GivenUserObtainsToken(string username)
    {
        _userTokens[username] = await RequestUserTokenAsync(username);
    }

    [When("雙呼叫者票服務 {string} 以已簽章請求建立訂單 {string}，Idempotency-Key 為 {string}")]
    public async Task WhenServiceCreatesOrder(string clientId, string label, string key)
    {
        _serviceToken ??= await RequestServiceTokenAsync(clientId);
        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(clientId));
        var request = await SignedHttp.BuildSignedAsync(
            HttpMethod.Post, OrdersUri, _serviceToken, Runtime.SigningKey(clientId), OrderJson(DefaultItem), key);
        await SendAsync(client, request, label);
    }

    [When("雙呼叫者票使用者 {string} 以使用者 Token 建立訂單 {string}，Idempotency-Key 為 {string}")]
    public Task WhenUserCreatesOrder(string username, string label, string key)
    {
        var body = OrderJson(DefaultItem);
        _lastUserCreate = (username, key, body);
        return UserCreateAsync(_userTokens[username], key, body, label);
    }

    [When("雙呼叫者票使用者 {string} 以原使用者 Token 建立訂單 {string}，Idempotency-Key 為 {string}")]
    public Task WhenUserWithRevokedTokenCreatesOrder(string username, string label, string key)
    {
        return UserCreateAsync(_userTokens[username], key, OrderJson(DefaultItem), label);
    }

    [When("雙呼叫者票使用者以相同 Idempotency-Key 與新 nonce 重送上一筆建立訂單")]
    public Task WhenUserResendsLastCreate()
    {
        _lastUserCreate.Should().NotBeNull("必須先送出使用者建立訂單請求");
        var (username, key, body) = _lastUserCreate!.Value;
        return UserCreateAsync(_userTokens[username], key, body, label: null);
    }

    [When("雙呼叫者票持服務 Token 的呼叫端不附用戶端憑證，送出建立訂單請求")]
    public async Task WhenServiceTokenOnUserPath()
    {
        _serviceToken.Should().NotBeNullOrEmpty("必須先由服務建立訂單以取得服務 Token");
        await UserCreateAsync(_serviceToken!, Guid.NewGuid().ToString(), OrderJson(DefaultItem), label: null);
    }

    [When("雙呼叫者票以使用者 {string} 的使用者 Token 附服務憑證與已簽章請求送出建立訂單")]
    public async Task WhenUserTokenOnServicePath(string username)
    {
        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(ServiceClientId));
        var request = await SignedHttp.BuildSignedAsync(
            HttpMethod.Post, OrdersUri, _userTokens[username], Runtime.SigningKey(ServiceClientId), OrderJson(DefaultItem), Guid.NewGuid().ToString());
        await SendAsync(client, request, label: null);
    }

    [When("雙呼叫者票使用者 {string} 以使用者 Token 查詢訂單 {string}")]
    public async Task WhenUserReadsOrder(string username, string label)
    {
        _orderIds.Should().ContainKey(label, "必須先成功建立該訂單");
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{OrdersUri}/me/{_orderIds[label]}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _userTokens[username]);
        using var client = SignedHttp.CreateClient(Runtime, certificate: null);
        await SendAsync(client, request, label: null);
    }

    [When("雙呼叫者票停用使用者 {string}")]
    public void WhenUserIsDisabled(string username)
    {
        Runtime.Registry.DisableUser(username);
    }

    [Then("雙呼叫者票最近一次請求回應 {int}")]
    public void ThenLastResponseIs(int statusCode)
    {
        _lastStatus.Should().Be((HttpStatusCode)statusCode, _lastBody);
    }

    [Then("雙呼叫者票最近一次請求標示為重播")]
    public void ThenLastResponseIsReplayed()
    {
        _lastReplayed.Should().BeTrue(_lastBody);
    }

    [Then("雙呼叫者票重播回應的訂單編號與 {string} 相同")]
    public void ThenReplayReturnsSameOrder(string label)
    {
        _orderIds.Should().ContainKey(label);
        JsonDocument.Parse(_lastBody).RootElement.GetProperty("orderId").GetGuid().Should().Be(_orderIds[label]);
    }

    [Then("雙呼叫者票訂單 {string} 的持有者為 {string}")]
    public void ThenOrderOwnerIs(string label, string owner)
    {
        _orderOwners.Should().ContainKey(label);
        _orderOwners[label].Should().Be(owner);
    }

    [Then("雙呼叫者票業務 API 建立訂單總數為 {int}")]
    public void ThenOrderCountIs(int count)
    {
        Runtime.Orders.Count.Should().Be(count);
    }

    [Then("雙呼叫者票第 {int} 項驗收已勾選")]
    public void ThenIssueCheckboxIsChecked(int index)
    {
        CheckboxAt(index).Should().StartWith("- [x] ");
    }

    [Then("雙呼叫者票的狀態為 resolved")]
    public void ThenIssueStatusIsResolved()
    {
        IssueText.Should().Contain("**Status:** resolved");
    }

    [Then("雙呼叫者票的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    private async Task UserCreateAsync(string bearer, string key, string body, string? label)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, OrdersUri)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", key);

        // 使用者寫入沒有請求簽章，每次請求以新的 X-Request-Nonce 防重放（05 單）。
        request.Headers.TryAddWithoutValidation("X-Request-Nonce", SignedHttp.NewNonce());

        // 使用者路徑不附用戶端憑證：每次新連線，確保 TLS 不帶任何用戶端憑證。
        using var client = SignedHttp.CreateClient(Runtime, certificate: null);
        await SendAsync(client, request, label);
    }

    private async Task SendAsync(HttpClient client, HttpRequestMessage request, string? label)
    {
        using var response = await client.SendAsync(request);
        _lastStatus = response.StatusCode;
        _lastBody = await response.Content.ReadAsStringAsync();
        _lastReplayed = response.Headers.TryGetValues("Idempotent-Replayed", out var values) && values.Contains("true");
        if (response.StatusCode == HttpStatusCode.Created && label is not null)
        {
            using var document = JsonDocument.Parse(_lastBody);
            _orderIds[label] = document.RootElement.GetProperty("orderId").GetGuid();
            _orderOwners[label] = document.RootElement.GetProperty("clientId").GetString()!;
        }
    }

    private static async Task<string> RequestUserTokenAsync(string username)
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

    private static async Task<string> RequestServiceTokenAsync(string clientId)
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

    private static string OrderJson(string item)
        => $$"""{"orderReference":"{{Guid.NewGuid():N}}","item":"{{item}}","quantity":1}""";

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
