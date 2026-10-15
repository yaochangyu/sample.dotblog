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

/// <summary>02 單：使用者 Token 簽發與使用者驗證路徑。全部經真實 TLS、真實密碼授權（password grant）與內省驗證。</summary>
[Binding]
public sealed class UserTokenVerificationPathSteps
{
    private const string IssueRelativePath = ".scratch/dual-caller-authentication/issues/02-user-token-and-verification-path.md";
    private const string TestUserId = "test-user-1";

    private static SpikeRuntime? _otherEnvironment;

    private string? _token;
    private HttpStatusCode? _status;
    private string _responseBody = string.Empty;
    private string? _tokenErrorCode;
    private string _introspectionBody = string.Empty;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    private static Uri OrdersUri => new($"https://localhost:{Runtime.OrdersApi.Port}/orders");

    [Given("使用者呼叫端以測試使用者帳號取得使用者 Token")]
    public async Task GivenUserCallerHasToken()
    {
        var (status, body) = await RequestUserTokenAsync(Runtime, UserCallers.TestUserPassword);
        status.Should().Be(HttpStatusCode.OK, body);
        _token = JsonDocument.Parse(body).RootElement.GetProperty("access_token").GetString();
        _token.Should().NotBeNullOrEmpty();
    }

    [Given("另一個環境 {string} 已由使用者取得使用者 Token")]
    public async Task GivenOtherEnvironmentIssuesUserToken(string environmentName)
    {
        _otherEnvironment = await SpikeRuntime.StartAsync(
            SpikeEnvironment.FreeTcpPort(),
            SpikeEnvironment.FreeTcpPort(),
            environmentName: environmentName);
        var (status, body) = await RequestUserTokenAsync(_otherEnvironment, UserCallers.TestUserPassword);
        status.Should().Be(HttpStatusCode.OK, body);
        _token = JsonDocument.Parse(body).RootElement.GetProperty("access_token").GetString();
    }

    [Given("服務 {string} 已以憑證取得服務 Token")]
    public async Task GivenServiceHasToken(string clientId)
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
        _token = JsonDocument.Parse(body).RootElement.GetProperty("access_token").GetString();
    }

    [When("使用者呼叫端以測試使用者帳號與錯誤密碼要求使用者 Token")]
    public async Task WhenUserCallerRequestsTokenWithWrongPassword()
    {
        var (status, body) = await RequestUserTokenAsync(Runtime, "wrong-password");
        _status = status;
        _responseBody = body;
        _tokenErrorCode = ReadErrorCode(body);
    }

    [When("查詢使用者 Token 的內省結果")]
    public async Task WhenIntrospectUserToken()
    {
        _token.Should().NotBeNullOrEmpty();
        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(SpikeRuntime.ResourceClientId));
        using var response = await client.PostAsync(
            new Uri(Runtime.AuthServer.Issuer, "connect/introspect"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["token"] = _token!,
                ["token_type_hint"] = "access_token",
                ["client_id"] = UserCallers.IntrospectionClientId,
            }));
        _introspectionBody = await response.Content.ReadAsStringAsync();
    }

    [When("使用者呼叫端不附用戶端憑證與簽章，送出建立訂單請求")]
    public async Task WhenUserCallerCreatesOrderWithoutCertificate()
    {
        _token.Should().NotBeNullOrEmpty();
        await SendCreateOrderAsync(_token!);
    }

    [When("使用者呼叫端以偽造的使用者 Token 送出建立訂單請求")]
    public async Task WhenUserCallerCreatesOrderWithForgedToken()
    {
        await SendCreateOrderAsync("forged-user-token-value");
    }

    [When("測試使用者被停用")]
    public void WhenTestUserIsDisabled()
    {
        Runtime.Registry.DisableUser(TestUserId);
    }

    [Then("使用者 Token 內省結果為 active，目標 API 為 {string}，呼叫者類型為 {string}，使用者為 {string}")]
    public void ThenIntrospectionShowsUserToken(string audience, string callerType, string subject)
    {
        using var document = JsonDocument.Parse(_introspectionBody);
        var root = document.RootElement;
        root.GetProperty("active").GetBoolean().Should().BeTrue(_introspectionBody);
        root.GetProperty("sub").GetString().Should().Be(subject);
        root.GetProperty(UserCallers.CallerTypeClaim).GetString().Should().Be(callerType);
        AudienceOf(root).Should().Contain(audience);
    }

    [Then("使用者呼叫端最近一次請求回應 {int}")]
    public void ThenUserCallerLastResponseIs(int statusCode)
    {
        _status.Should().Be((HttpStatusCode)statusCode, _responseBody);
    }

    [Then("使用者呼叫端建立的訂單以使用者 {string} 身分提交")]
    public void ThenOrderSubmittedAsUser(string userId)
    {
        _status.Should().Be(HttpStatusCode.Created, _responseBody);
        using var document = JsonDocument.Parse(_responseBody);
        document.RootElement.GetProperty("clientId").GetString().Should().Be(userId);
    }

    [Then("授權伺服器拒絕使用者 Token 要求，錯誤代碼為 {string}")]
    public void ThenTokenRequestRejected(string errorCode)
    {
        _status.Should().Be(HttpStatusCode.BadRequest, _responseBody);
        _tokenErrorCode.Should().Be(errorCode, _responseBody);
    }

    [Then("02 單第 {int} 項驗收已勾選")]
    public void ThenIssueCheckboxIsChecked(int index)
    {
        CheckboxAt(index).Should().StartWith("- [x] ");
    }

    [Then("02 單的狀態為 resolved")]
    public void ThenIssueStatusIsResolved()
    {
        IssueText.Should().Contain("**Status:** resolved");
    }

    [AfterScenario]
    public static async Task DisposeOtherEnvironment()
    {
        if (_otherEnvironment is not null)
        {
            await _otherEnvironment.DisposeAsync();
            _otherEnvironment = null;
        }
    }

    private async Task SendCreateOrderAsync(string bearer)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, OrdersUri)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { orderReference = SignedHttp.NewNonce(), item = "book", quantity = 1 }),
                Encoding.UTF8,
                "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString());

        // 使用者路徑不附用戶端憑證：每次新連線，確保 TLS 不帶任何用戶端憑證。
        using var client = SignedHttp.CreateClient(Runtime, certificate: null);
        using var response = await client.SendAsync(request);
        _status = response.StatusCode;
        _responseBody = await response.Content.ReadAsStringAsync();
    }

    private static async Task<(HttpStatusCode Status, string Body)> RequestUserTokenAsync(SpikeRuntime runtime, string password)
    {
        using var client = SignedHttp.CreateClient(runtime, certificate: null);
        using var response = await client.PostAsync(
            new Uri(runtime.AuthServer.Issuer, "connect/token"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = UserCallers.ClientId,
                ["username"] = UserCallers.TestUserId,
                ["password"] = password,
            }));
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private static string? ReadErrorCode(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.TryGetProperty("error", out var error) ? error.GetString() : null;
    }

    /// <summary>內省回應的 aud 可能是字串或陣列；兩種都接受。</summary>
    private static IEnumerable<string> AudienceOf(JsonElement root)
    {
        if (!root.TryGetProperty("aud", out var aud))
        {
            return [];
        }

        return aud.ValueKind == JsonValueKind.Array
            ? aud.EnumerateArray().Select(item => item.GetString()!).ToList()
            : [aud.GetString()!];
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
