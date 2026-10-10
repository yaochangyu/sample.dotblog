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

/// <summary>05 單：Client 可執行操作（scope 白名單）與業務資料範圍的實際呼叫情境。</summary>
[Binding]
public sealed class ClientAndDataAuthorizationSteps
{
    /// <summary>每次取用都產生新的業務識別（orderReference），使各 Scenario 建立的訂單互不去重（06 單）。</summary>
    private static string OrderJson() => $$"""{"orderReference":"{{Guid.NewGuid():N}}","item":"book","quantity":1}""";
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/05-client-and-data-authorization.md";
    private const string ContractRelativePath = "poc/AuthSpike/doc/openapi.yml";

    private readonly Dictionary<string, string> _tokens = new();
    private readonly Dictionary<string, Guid> _orderIds = new();
    private HttpStatusCode? _tokenStatus;
    private string _tokenBody = string.Empty;
    private HttpStatusCode? _lastStatus;
    private string _lastBody = string.Empty;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    [Given("05 單的實作紀錄可讀取")]
    public void GivenIssueRecordIsReadable()
    {
        File.Exists(Path.Combine(RepoRoot, IssueRelativePath)).Should().BeTrue();
    }

    [Given("呼叫端 {string} 以授權範圍 {string} 取得 Token")]
    public async Task GivenCallerObtainsScopedToken(string clientId, string scope)
    {
        var (status, body) = await RequestTokenAsync(clientId, scope);
        status.Should().Be(HttpStatusCode.OK, body);
        using var document = JsonDocument.Parse(body);
        _tokens[clientId] = document.RootElement.GetProperty("access_token").GetString()!;
    }

    [When("呼叫端 {string} 要求授權範圍 {string} 的 Token")]
    public async Task WhenCallerRequestsScopedToken(string clientId, string scope)
    {
        var response = await RequestTokenAsync(clientId, scope);
        _tokenStatus = response.Status;
        _tokenBody = response.Body;
    }

    [Then("授權伺服器拒絕授權範圍要求，錯誤為 {string}")]
    public void ThenScopeRequestIsRejected(string error)
    {
        _tokenStatus.Should().Be(HttpStatusCode.BadRequest, _tokenBody);
        _tokenBody.Should().NotContain("access_token");
        using var document = JsonDocument.Parse(_tokenBody);
        document.RootElement.GetProperty("error").GetString().Should().Be(error);
    }

    [Given("呼叫端 {string} 建立訂單 {string}")]
    [When("呼叫端 {string} 建立訂單 {string}")]
    public async Task CallerCreatesOrder(string clientId, string name)
    {
        await CreateOrderAsync(clientId, name, "/orders", OrderJson());
    }

    [When("呼叫端 {string} 建立訂單 {string}，Body 與查詢參數宣稱 clientId 為 {string}")]
    public async Task CallerCreatesOrderWithClaimedClientId(string clientId, string name, string claimedClientId)
    {
        var body = $$"""{"orderReference":"{{Guid.NewGuid():N}}","item":"book","quantity":1,"clientId":"{{claimedClientId}}"}""";
        await CreateOrderAsync(clientId, name, $"/orders?clientId={claimedClientId}", body);
    }

    [When("呼叫端 {string} 取消訂單 {string}")]
    public async Task CallerCancelsOrder(string clientId, string name)
    {
        await SendAsync(clientId, HttpMethod.Post, $"/orders/{_orderIds[name]}/cancel", "{}");
    }

    [When("呼叫端 {string} 取消訂單 {string}，並附帶 X-Client-Id 標頭 {string}")]
    public async Task CallerCancelsOrderWithClaimedHeader(string clientId, string name, string claimedClientId)
    {
        await SendAsync(clientId, HttpMethod.Post, $"/orders/{_orderIds[name]}/cancel", "{}", claimedClientId);
    }

    [Then("最近一次操作回應 {int}")]
    public void ThenLastOperationReturns(int statusCode)
    {
        _lastStatus.Should().Be((HttpStatusCode)statusCode, _lastBody);
    }

    [Then("呼叫端 {string} 查詢訂單 {string} 回應 {int}")]
    public async Task ThenCallerGetsOrder(string clientId, string name, int statusCode)
    {
        await SendAsync(clientId, HttpMethod.Get, $"/orders/{_orderIds[name]}", null);
        _lastStatus.Should().Be((HttpStatusCode)statusCode, _lastBody);
    }

    [Then("呼叫端 {string} 查詢訂單 {string} 的狀態為 {string}")]
    public async Task ThenCallerSeesOrderStatus(string clientId, string name, string status)
    {
        await SendAsync(clientId, HttpMethod.Get, $"/orders/{_orderIds[name]}", null);
        _lastStatus.Should().Be(HttpStatusCode.OK, _lastBody);
        using var document = JsonDocument.Parse(_lastBody);
        document.RootElement.GetProperty("status").GetString().Should().Be(status);
    }

    [Then("呼叫端 {string} 查詢訂單 {string} 的 clientId 為 {string}")]
    public async Task ThenCallerSeesOrderOwner(string clientId, string name, string ownerClientId)
    {
        await SendAsync(clientId, HttpMethod.Get, $"/orders/{_orderIds[name]}", null);
        _lastStatus.Should().Be(HttpStatusCode.OK, _lastBody);
        using var document = JsonDocument.Parse(_lastBody);
        document.RootElement.GetProperty("clientId").GetString().Should().Be(ownerClientId);
    }

    [Then("建立訂單契約檔包含 {string}")]
    public void ThenContractContains(string expected)
    {
        File.ReadAllText(Path.Combine(RepoRoot, ContractRelativePath)).Should().Contain(expected);
    }

    [Then("05 單第 {int} 項驗收已勾選")]
    public void ThenIssueCheckboxIsChecked(int index)
    {
        CheckboxAt(index).Should().StartWith("- [x] ");
    }

    [Then("05 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
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

    private async Task CreateOrderAsync(string clientId, string name, string pathAndQuery, string body)
    {
        await SendAsync(clientId, HttpMethod.Post, pathAndQuery, body);
        _lastStatus.Should().Be(HttpStatusCode.Created, _lastBody);
        using var document = JsonDocument.Parse(_lastBody);
        _orderIds[name] = document.RootElement.GetProperty("orderId").GetGuid();
    }

    /// <summary>以 clientId 的 Token 與簽章金鑰送出已簽章請求；連線使用同一 Client 的用戶端憑證。</summary>
    private async Task SendAsync(string clientId, HttpMethod method, string pathAndQuery, string? body, string? spoofedClientId = null)
    {
        var request = new HttpRequestMessage(method, new Uri($"https://localhost:{Runtime.OrdersApi.Port}{pathAndQuery}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[clientId]);
        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString());
        }

        if (spoofedClientId is not null)
        {
            request.Headers.TryAddWithoutValidation("X-Client-Id", spoofedClientId);
        }

        await SignedHttp.SignNowAsync(request, Runtime.SigningKey(clientId));

        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(clientId));
        using var response = await client.SendAsync(request);
        _lastStatus = response.StatusCode;
        _lastBody = await response.Content.ReadAsStringAsync();
    }

    private static async Task<ApiResponse> RequestTokenAsync(string clientId, string scope)
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

        return new ApiResponse(response.StatusCode, await response.Content.ReadAsStringAsync());
    }

}
