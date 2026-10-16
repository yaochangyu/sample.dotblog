using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthSpike.Hosting;
using AuthSpike.Tests.Support;
using AuthSpike.Verification;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>04 單：依呼叫者類型授權。全部經真實 TLS、真實 client_credentials／password grant 與建立訂單 API。</summary>
[Binding]
public sealed class CallerTypeAuthorizationSteps
{
    private const string IssueRelativePath = ".scratch/dual-caller-authentication/issues/04-caller-type-authorization.md";

    private readonly Dictionary<string, string> _userTokens = new();
    private readonly Dictionary<string, string> _serviceTokens = new();
    private readonly Dictionary<string, Guid> _orderIds = new();
    private HttpStatusCode? _status;
    private string _responseBody = string.Empty;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    private static Uri OrdersUri => new($"https://localhost:{Runtime.OrdersApi.Port}/orders");

    [Given("使用者 {string} 取得使用者 Token（04 單）")]
    public async Task GivenUserToken(string username)
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
        _userTokens[username] = JsonDocument.Parse(body).RootElement.GetProperty("access_token").GetString()!;
    }

    [Given("服務 {string} 以授權範圍 {string} 取得服務 Token（04 單）")]
    public async Task GivenServiceToken(string clientId, string scope)
    {
        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(clientId));
        using var response = await client.PostAsync(
            new Uri(Runtime.AuthServer.Issuer, "connect/token"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["scope"] = scope,
            }));
        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);
        _serviceTokens[clientId] = JsonDocument.Parse(body).RootElement.GetProperty("access_token").GetString()!;
    }

    [Given("使用者 {string} 建立訂單 {string}（04 單）")]
    public async Task GivenUserCreatesOrder(string username, string name)
    {
        var request = PlainRequest(HttpMethod.Post, OrdersUri, _userTokens[username], OrderBody());
        request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString());
        await SendAsync(request, certificate: null);
        _status.Should().Be(HttpStatusCode.Created, _responseBody);
        Remember(name);
    }

    [Given("服務 {string} 以已簽章請求建立訂單 {string}（04 單）")]
    public async Task GivenServiceCreatesOrder(string clientId, string name)
    {
        var request = await SignedHttp.BuildSignedAsync(HttpMethod.Post, OrdersUri, _serviceTokens[clientId], Runtime.SigningKey(clientId), OrderBody(), Guid.NewGuid().ToString());
        await SendAsync(request, Runtime.ClientCertificate(clientId));
        _status.Should().Be(HttpStatusCode.Created, _responseBody);
        Remember(name);
    }

    [When("使用者 {string} 以使用者 Token 查詢訂單 {string}（04 單）")]
    public async Task WhenUserGetsOrder(string username, string name)
    {
        await SendAsync(PlainRequest(HttpMethod.Get, new Uri($"{OrdersUri}/{_orderIds[name]}"), _userTokens[username], null), certificate: null);
    }

    [When("使用者 {string} 以使用者 Token 取消訂單 {string}（04 單）")]
    public async Task WhenUserCancelsOrder(string username, string name)
    {
        await SendAsync(PlainRequest(HttpMethod.Post, new Uri($"{OrdersUri}/{_orderIds[name]}/cancel"), _userTokens[username], "{}"), certificate: null);
    }

    [When("服務 {string} 以已簽章請求查詢訂單 {string}（04 單）")]
    public async Task WhenServiceGetsOrderSigned(string clientId, string name)
    {
        var request = await SignedHttp.BuildSignedAsync(HttpMethod.Get, new Uri($"{OrdersUri}/{_orderIds[name]}"), _serviceTokens[clientId], Runtime.SigningKey(clientId));
        await SendAsync(request, Runtime.ClientCertificate(clientId));
    }

    [When("服務 {string} 以已簽章請求查詢使用者專屬訂單 {string}（04 單）")]
    public async Task WhenServiceGetsOrderOnUserOnlyPathSigned(string clientId, string name)
    {
        var request = await SignedHttp.BuildSignedAsync(HttpMethod.Get, new Uri($"{OrdersUri}/me/{_orderIds[name]}"), _serviceTokens[clientId], Runtime.SigningKey(clientId));
        await SendAsync(request, Runtime.ClientCertificate(clientId));
    }

    [Then("04 單最近一次請求回應 {int}")]
    public void ThenLastRequestReturns(int statusCode)
    {
        _status.Should().Be((HttpStatusCode)statusCode, _responseBody);
    }

    [Then("04 單最近一次回應不含訂單 {string}（04 單）")]
    public void ThenLastResponseHidesOrder(string name)
    {
        _responseBody.Should().NotContain(_orderIds[name].ToString());
    }

    [Then("04 單訂單 {string} 由 {string} 持有的狀態為 {string}（04 單）")]
    public void ThenOrderStatusIs(string name, string owner, string status)
    {
        Runtime.Orders.TryGet(_orderIds[name], owner, out var order).Should().BeTrue();
        order!.Status.Should().Be(status);
    }

    [Then("依呼叫者類型授權票第 {int} 項驗收已勾選")]
    public void ThenIssueCheckboxIsChecked(int index)
    {
        CheckboxAt(index).Should().StartWith("- [x] ");
    }

    [Then("依呼叫者類型授權票的狀態為 resolved")]
    public void ThenIssueStatusIsResolved()
    {
        IssueText.Should().Contain("**Status:** resolved");
    }

    [Then("依呼叫者類型授權票的實作紀錄標註使用者資料範圍為待使用者確認")]
    public void ThenImplementationRecordMarksUserScopeProvisional()
    {
        IssueText.Split('\n').Should().Contain(line => line.Contains("使用者資料範圍") && line.Contains("待使用者確認"));
    }

    private void Remember(string name)
    {
        using var document = JsonDocument.Parse(_responseBody);
        _orderIds[name] = document.RootElement.GetProperty("orderId").GetGuid();
    }

    private async Task SendAsync(HttpRequestMessage request, X509Certificate2? certificate)
    {
        using var client = SignedHttp.CreateClient(Runtime, certificate);
        using var response = await client.SendAsync(request);
        _status = response.StatusCode;
        _responseBody = await response.Content.ReadAsStringAsync();
    }

    private static HttpRequestMessage PlainRequest(HttpMethod method, Uri uri, string bearer, string? body)
    {
        var request = new HttpRequestMessage(method, uri);
        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return request;
    }

    /// <summary>每次取用都產生新的業務識別（orderReference），使各 Scenario 建立的訂單互不去重。</summary>
    private static string OrderBody()
        => JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["orderReference"] = SignedHttp.NewNonce(),
            ["item"] = "book",
            ["quantity"] = 1,
        });

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
