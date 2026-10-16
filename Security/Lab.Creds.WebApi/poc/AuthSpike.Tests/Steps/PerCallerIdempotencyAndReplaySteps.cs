using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthSpike.Hosting;
using AuthSpike.OrdersApi;
using AuthSpike.Tests.Support;
using AuthSpike.Verification;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>05 單：冪等與防重放依呼叫者身分分開。全部經真實 TLS、真實 password／client_credentials grant 與建立訂單 API。</summary>
[Binding]
public sealed class PerCallerIdempotencyAndReplaySteps
{
    private const string IssueRelativePath = ".scratch/dual-caller-authentication/issues/05-per-caller-idempotency-and-replay.md";
    private const string UserNonceHeader = "X-Request-Nonce";
    private static readonly TimeSpan StepTimeout = TimeSpan.FromSeconds(10);

    private readonly Dictionary<string, string> _userTokens = new();
    private readonly Dictionary<string, string> _serviceTokens = new();
    private readonly Dictionary<string, Guid> _firstOrderIds = new();
    private readonly List<(string Username, string Key, string Body)> _userAttempts = [];
    private readonly List<(string ClientId, string Key, string Body, string Nonce)> _serviceAttempts = [];
    private CommitHold? _hold;
    private Task<ApiResponse>? _background;
    private ApiResponse? _last;
    private List<ApiResponse> _race = [];
    private string? _lastRef;
    private Guid? _lastOrderId;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    private static Uri OrdersUri => new($"https://localhost:{Runtime.OrdersApi.Port}/orders");

    [Given("使用者 {string} 取得使用者 Token（05 單）")]
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

    [Given("服務 {string} 以授權範圍 {string} 取得服務 Token（05 單）")]
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

    [Given("使用者業務提交將暫停（05 單）")]
    public void GivenUserCommitHeld()
    {
        _hold = Runtime.Orders.HoldNextCommit();
    }

    [When("使用者 {string} 以 Idempotency-Key {string} 送出業務識別 {string}，品項 {string}，數量 {int}（05 單）")]
    public async Task WhenUserSubmits(string username, string key, string reference, string item, int quantity)
    {
        var body = OrderBody(reference, item, quantity);
        _userAttempts.Add((username, key, body));
        _last = await SendUserAsync(username, key, body, SignedHttp.NewNonce());
        RememberLast(reference, _last);
    }

    [When("使用者 {string} 以 Idempotency-Key {string} 於背景送出業務識別 {string}，品項 {string}，數量 {int}（05 單）")]
    public void WhenUserSubmitsInBackground(string username, string key, string reference, string item, int quantity)
    {
        var body = OrderBody(reference, item, quantity);
        _userAttempts.Add((username, key, body));
        _background = SendUserAsync(username, key, body, SignedHttp.NewNonce());
    }

    [When("等待使用者背景送出進入業務提交（05 單）")]
    public async Task WhenBackgroundUserEntersCommit()
    {
        _hold.Should().NotBeNull("必須先設定暫停的業務提交");
        await _hold!.Entered.WaitAsync(StepTimeout);
    }

    [When("放行使用者暫停的業務提交（05 單）")]
    public void WhenHeldUserCommitReleased()
    {
        _hold.Should().NotBeNull();
        _hold!.Release();
    }

    [When("使用者 {string} 以相同 Idempotency-Key 與新 nonce 重送上一筆業務操作（05 單）")]
    public async Task WhenUserRetriesWithNewNonce(string username)
    {
        var (_, key, body) = _userAttempts[^1];
        _last = await SendUserAsync(username, key, body, SignedHttp.NewNonce());
        RememberLast(_lastRef!, _last);
    }

    [When("使用者 {string} 不附 X-Request-Nonce 送出業務識別 {string}，品項 {string}，數量 {int}（05 單）")]
    public async Task WhenUserWritesWithoutNonce(string username, string reference, string item, int quantity)
    {
        _last = await SendUserAsync(username, SignedHttp.NewNonce(), OrderBody(reference, item, quantity), nonce: null);
        RememberLast(reference, _last);
    }

    [When("使用者 {string} 以 nonce {string} 送出業務識別 {string}，品項 {string}，數量 {int}（05 單）")]
    public async Task WhenUserWritesWithNonce(string username, string nonce, string reference, string item, int quantity)
    {
        var body = OrderBody(reference, item, quantity);
        _userAttempts.Add((username, Guid.NewGuid().ToString(), body));
        _last = await SendUserAsync(username, _userAttempts[^1].Key, body, nonce);
        RememberLast(reference, _last);
    }

    [When("使用者 {string} 以 nonce {string} 重送上一筆使用者寫入請求（05 單）")]
    public async Task WhenUserReplaysNonce(string username, string nonce)
    {
        var (_, key, body) = _userAttempts[^1];
        _last = await SendUserAsync(username, key, body, nonce);
    }

    [When("使用者 {string} 以 10 組新 nonce 與新 Idempotency-Key 併發送出業務識別 {string}，品項 {string}，數量 {int}（05 單）")]
    public async Task WhenUserRaces(string username, string reference, string item, int quantity)
    {
        var body = OrderBody(reference, item, quantity);
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => SendUserAsync(username, Guid.NewGuid().ToString(), body, SignedHttp.NewNonce()))
            .ToArray();
        _race = (await Task.WhenAll(tasks)).ToList();
        _lastRef = reference;
    }

    [When("服務 {string} 以 Idempotency-Key {string} 已簽章送出業務識別 {string}，品項 {string}，數量 {int}（05 單）")]
    public async Task WhenServiceSubmitsSigned(string clientId, string key, string reference, string item, int quantity)
    {
        var body = OrderBody(reference, item, quantity);
        _serviceAttempts.Add((clientId, key, body, SignedHttp.NewNonce()));
        _last = await SendServiceAsync(clientId, key, body, SignedHttp.NewNonce());
        RememberLast(reference, _last);
    }

    [When("服務 {string} 以 nonce {string} 已簽章送出業務識別 {string}，Idempotency-Key 為 {string}，品項 {string}，數量 {int}（05 單）")]
    public async Task WhenServiceSubmitsSignedWithNonce(string clientId, string nonce, string reference, string key, string item, int quantity)
    {
        var body = OrderBody(reference, item, quantity);
        _serviceAttempts.Add((clientId, key, body, nonce));
        _last = await SendServiceAsync(clientId, key, body, nonce);
        RememberLast(reference, _last);
    }

    [When("服務 {string} 重送上一筆已簽章請求（相同 nonce）（05 單）")]
    public async Task WhenServiceReplaysSignedRequest(string clientId)
    {
        var (_, key, body, nonce) = _serviceAttempts[^1];
        _last = await SendServiceAsync(clientId, key, body, nonce);
    }

    [Then("05 單最近一次請求回應 {int}")]
    public void ThenLastRequestReturns(int statusCode)
    {
        _last.Should().NotBeNull();
        _last!.Status.Should().Be((HttpStatusCode)statusCode, _last.Body);
    }

    [Then("05 單最近一次回應錯誤代碼為 {string}")]
    public void ThenLastErrorCodeIs(string errorCode)
    {
        using var document = JsonDocument.Parse(_last!.Body);
        document.RootElement.GetProperty("error").GetString().Should().Be(errorCode, _last.Body);
    }

    [Then("05 單最近一次請求標示為重播")]
    public void ThenLastRequestIsReplayed()
    {
        _last!.Replayed.Should().BeTrue(_last.Body);
    }

    [Then("05 單重播回應的訂單編號與首次建立相同")]
    public void ThenReplayReturnsSameOrder()
    {
        _lastOrderId.Should().Be(_firstOrderIds[_lastRef!]);
    }

    [Then("使用者背景送出的請求回應 {int}（05 單）")]
    public async Task ThenBackgroundUserRequestReturns(int statusCode)
    {
        _background.Should().NotBeNull();
        var result = await _background!.WaitAsync(StepTimeout);
        result.Status.Should().Be((HttpStatusCode)statusCode, result.Body);
    }

    [Then("05 單業務識別 {string} 在使用者 {string} 範圍的訂單數為 {int}")]
    public void ThenUserScopeCountIs(string reference, string username, int count)
    {
        Runtime.Orders.CountByOrderReference(VerifiedCaller.BusinessScopeOf(CallerType.User, username), reference).Should().Be(count);
    }

    [Then("05 單業務識別 {string} 在服務 {string} 範圍的訂單數為 {int}")]
    public void ThenServiceScopeCountIs(string reference, string clientId, int count)
    {
        Runtime.Orders.CountByOrderReference(VerifiedCaller.BusinessScopeOf(CallerType.Service, clientId), reference).Should().Be(count);
    }

    [Then("05 單併發結果只包含回應 201 或 409")]
    public void ThenRaceOnlyCreatedOrInProgress()
    {
        _race.Select(result => result.Status).Should().OnlyContain(status => status == HttpStatusCode.Created || status == HttpStatusCode.Conflict);
    }

    [Then("05 單併發結果中建立的訂單編號只有 1 個")]
    public void ThenRaceCreatedOneOrder()
    {
        var created = _race.Where(result => result.Status == HttpStatusCode.Created)
            .Select(result => JsonDocument.Parse(result.Body).RootElement.GetProperty("orderId").GetGuid())
            .Distinct()
            .ToList();
        created.Should().HaveCount(1);
    }

    [Then("冪等與防重放票第 {int} 項驗收已勾選")]
    public void ThenIssueCheckboxIsChecked(int index)
    {
        CheckboxAt(index).Should().StartWith("- [x] ");
    }

    [Then("冪等與防重放票的狀態為 resolved")]
    public void ThenIssueStatusIsResolved()
    {
        IssueText.Should().Contain("**Status:** resolved");
    }

    [Then("冪等與防重放票的實作紀錄標註防重放機制為 lab 暫定待使用者確認")]
    public void ThenImplementationRecordMarksReplayProvisional()
    {
        IssueText.Split('\n').Should().Contain(line => line.Contains("防重放") && line.Contains("lab 暫定") && line.Contains("待使用者確認"));
    }

    private void RememberLast(string reference, ApiResponse response)
    {
        _lastRef = reference;
        if (response.Status != HttpStatusCode.Created)
        {
            _lastOrderId = null;
            return;
        }

        _lastOrderId = JsonDocument.Parse(response.Body).RootElement.GetProperty("orderId").GetGuid();
        _firstOrderIds.TryAdd(reference, _lastOrderId.Value);
    }

    private async Task<ApiResponse> SendUserAsync(string username, string key, string body, string? nonce)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, OrdersUri)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _userTokens[username]);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", key);
        if (nonce is not null)
        {
            request.Headers.TryAddWithoutValidation(UserNonceHeader, nonce);
        }

        // 使用者路徑不附用戶端憑證：每次新連線，確保 TLS 不帶任何用戶端憑證。
        return await SendAsync(request, certificate: null);
    }

    private async Task<ApiResponse> SendServiceAsync(string clientId, string key, string body, string nonce)
    {
        var request = await SignedHttp.BuildSignedAsync(HttpMethod.Post, OrdersUri, _serviceTokens[clientId], Runtime.SigningKey(clientId), body, key, nonce: nonce);
        return await SendAsync(request, Runtime.ClientCertificate(clientId));
    }

    private static async Task<ApiResponse> SendAsync(HttpRequestMessage request, X509Certificate2? certificate)
    {
        using var client = SignedHttp.CreateClient(Runtime, certificate);
        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return new ApiResponse(response.StatusCode, text)
        {
            Replayed = response.Headers.Contains("Idempotent-Replayed"),
        };
    }

    private static string OrderBody(string reference, string item, int quantity)
        => JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["orderReference"] = reference,
            ["item"] = item,
            ["quantity"] = quantity,
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
