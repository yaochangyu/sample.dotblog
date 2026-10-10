using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthSpike.Hosting;
using AuthSpike.OrdersApi;
using AuthSpike.Signing;
using AuthSpike.Tests.Support;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>06 單：穩定業務識別、既有結果、處理中狀態、衝突拒絕、當機與回應遺失、跨執行個體併發只提交一次的實際呼叫情境。</summary>
[Binding]
public sealed class IdempotentBusinessCommitSteps
{
    private const string ClientId = "orders-client";
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/06-idempotent-business-commit.md";
    private const string OpenApiRelativePath = "poc/AuthSpike/doc/openapi.yml";
    private static readonly TimeSpan RetryWindow = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan StepTimeout = TimeSpan.FromSeconds(10);

    /// <summary>單次請求嘗試的參數；重試時只更換 nonce、時間、Token 或 Idempotency-Key，業務 Body 沿用。</summary>
    private sealed record Attempt(string Token, string Key, string Body, string Nonce, DateTimeOffset Created);

    private string? _token;
    private Attempt? _firstAttempt;
    private Attempt? _lastAttempt;
    private ApiResponse? _last;
    private Task<ApiResponse>? _background;
    private CommitHold? _hold;
    private List<ApiResponse> _race = [];
    private string? _firstOrderId;
    private string? _lastOrderId;
    private int _recordCount;
    private readonly List<HttpStatusCode> _queryStatuses = [];

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    [Given("下一次業務提交將暫停")]
    public void GivenNextCommitHeld()
    {
        _hold = Runtime.Orders.HoldNextCommit();
    }

    [Given("下一次業務提交於寫入前當機")]
    public void GivenNextCommitCrashesBeforeWrite()
    {
        Runtime.Orders.ArmFault(CommitFault.CrashBeforeCommit);
    }

    [Given("下一次業務提交於寫入後回應遺失")]
    public void GivenNextCommitLosesResponseAfterWrite()
    {
        Runtime.Orders.ArmFault(CommitFault.LoseResponseAfterCommit);
    }

    [When("簽章呼叫端 {string} 以 Idempotency-Key {string} 送出訂單 {string}，品項 {string}，數量 {int}")]
    public async Task WhenSignedOrderSent(string clientId, string key, string reference, string item, int quantity)
    {
        clientId.Should().Be(ClientId);
        await SendLastAsync(await NewAttemptAsync(key, OrderBody(reference, item, quantity)));
    }

    [When("簽章呼叫端 {string} 以 Idempotency-Key {string} 於背景送出訂單 {string}，品項 {string}，數量 {int}")]
    public async Task WhenSignedOrderSentInBackground(string clientId, string key, string reference, string item, int quantity)
    {
        clientId.Should().Be(ClientId);
        var attempt = await NewAttemptAsync(key, OrderBody(reference, item, quantity));
        _lastAttempt = attempt;
        _firstAttempt ??= attempt;
        _background = SendAsync(attempt, Runtime.OrdersApi.Port);
    }

    [When("等待背景送出進入業務提交")]
    public async Task WhenBackgroundEntersCommit()
    {
        _hold.Should().NotBeNull("必須先設定暫停的業務提交");
        await _hold!.Entered.WaitAsync(StepTimeout);
    }

    [When("放行暫停的業務提交")]
    public void WhenHeldCommitReleased()
    {
        _hold.Should().NotBeNull();
        _hold!.Release();
    }

    [When("簽章呼叫端 {string} 以 10 組新簽章與新 Idempotency-Key 併發送出訂單 {string}，品項 {string}，數量 {int}，交替送往兩個執行個體")]
    public async Task WhenRaceAcrossInstances(string clientId, string reference, string item, int quantity)
    {
        clientId.Should().Be(ClientId);
        var body = OrderBody(reference, item, quantity);
        var attempts = new List<Attempt>();
        for (var index = 0; index < 10; index++)
        {
            attempts.Add(await NewAttemptAsync(Guid.NewGuid().ToString(), body));
        }

        var primary = Runtime.OrdersApi.Port;
        var secondary = Runtime.SecondaryOrdersApi!.Port;
        var results = await Task.WhenAll(attempts.Select((attempt, index) =>
            SendAsync(attempt, index % 2 == 0 ? primary : secondary)));
        _race = results.ToList();
    }

    [When("簽章呼叫端以相同 Idempotency-Key 與新 nonce 重送上一筆訂單")]
    public Task WhenResendSameKeyNewNonce()
        => SendLastAsync(Renewed(_lastAttempt!));

    [When("簽章呼叫端以新的 Idempotency-Key {string} 重送上一筆訂單")]
    public Task WhenResendNewKey(string key)
        => SendLastAsync(Renewed(_lastAttempt!, key: key));

    [When("簽章呼叫端以新取得的 Token、新 nonce 與相同 Idempotency-Key 重送上一筆訂單")]
    public async Task WhenResendWithNewToken()
    {
        var token = await RequestTokenAsync();
        _token = token;
        await SendLastAsync(Renewed(_lastAttempt!, token: token));
    }

    [When("業務操作租約到期")]
    public void WhenLeaseExpires()
    {
        Runtime.Orders.ExpireLeasesForTest();
    }

    [When("業務操作的 Idempotency-Key 紀錄已過期")]
    public void WhenKeyRecordsExpire()
    {
        Runtime.Orders.ExpireKeyRecordsForTest();
    }

    [When("記錄目前的業務去重紀錄數")]
    public void WhenRecordDedupeCount()
    {
        _recordCount = Runtime.Orders.IdempotencyRecordCount;
    }

    [When("簽章呼叫端查詢上一筆訂單兩次")]
    public async Task WhenQueryLastOrderTwice()
    {
        _queryStatuses.Clear();
        for (var index = 0; index < 2; index++)
        {
            var (status, _) = await QueryOrderAsync(_lastOrderId!);
            _queryStatuses.Add(status);
        }
    }

    [Then("最近一次訂單請求回應 {int}")]
    public void ThenLastOrderRequestReturns(int statusCode)
    {
        _last.Should().NotBeNull("尚未送出任何訂單請求");
        _last!.Status.Should().Be((HttpStatusCode)statusCode, _last.Body);
    }

    [Then("最近一次訂單請求的錯誤代碼為 {string}")]
    public void ThenLastOrderErrorCodeIs(string code)
    {
        ReadJson(_last!.Body).GetProperty("error").GetString().Should().Be(code);
    }

    [Then("最近一次訂單請求的處理狀態為 {string}")]
    public void ThenLastOrderStatusIs(string status)
    {
        ReadJson(_last!.Body).GetProperty("status").GetString().Should().Be(status);
    }

    [Then("最近一次訂單請求標示為重播")]
    public void ThenLastOrderIsReplayed()
    {
        _last!.Replayed.Should().BeTrue("重播的既有結果應帶 Idempotent-Replayed 標頭");
    }

    [Then("重播回應的訂單編號與首次建立相同")]
    public void ThenReplayReturnsSameOrder()
    {
        _firstOrderId.Should().NotBeNull();
        _lastOrderId.Should().Be(_firstOrderId);
    }

    [Then("背景送出的訂單請求回應 {int}")]
    public async Task ThenBackgroundOrderReturns(int statusCode)
    {
        _background.Should().NotBeNull();
        var result = await _background!.WaitAsync(StepTimeout);
        result.Status.Should().Be((HttpStatusCode)statusCode, result.Body);
    }

    [Then("訂單 {string} 的業務訂單數為 {int}")]
    public void ThenOrderReferenceCountIs(string reference, int count)
    {
        Runtime.Orders.CountByOrderReference(ClientId, reference).Should().Be(count);
    }

    [Then("訂單 {string} 的內容為品項 {string} 數量 {int}")]
    public void ThenOrderContentIs(string reference, string item, int quantity)
    {
        Runtime.Orders.TryGetByOrderReference(ClientId, reference, out var order).Should().BeTrue();
        order!.Request.Item.Should().Be(item);
        order.Request.Quantity.Should().Be(quantity);
    }

    [Then("併發結果只包含回應 201 或 409")]
    public void ThenRaceOnlyCreatedOrInProgress()
    {
        _race.Should().NotBeEmpty();
        _race.Select(result => result.Status).Should()
            .OnlyContain(status => status == HttpStatusCode.Created || status == HttpStatusCode.Conflict,
                "併發請求不得回應 503 或其他錯誤");
    }

    [Then("併發結果中建立的訂單編號只有 1 個")]
    public void ThenRaceProducesSingleOrderId()
    {
        var orderIds = _race
            .Where(result => result.Status == HttpStatusCode.Created)
            .Select(result => ReadJson(result.Body).GetProperty("orderId").GetString())
            .ToList();
        orderIds.Should().NotBeEmpty();
        orderIds.Distinct().Should().ContainSingle();
    }

    [Then("Idempotency-Key {string} 的保存至時間不早於首次送出加 10 分鐘")]
    public void ThenKeyRetentionCoversRetryWindow(string key)
    {
        var retainUntil = Runtime.Orders.KeyRetainUntil(ClientId, key);
        retainUntil.Should().NotBeNull();
        retainUntil!.Value.Should().BeOnOrAfter(_firstAttempt!.Created.Add(RetryWindow));
    }

    [Then("重送使用的 Token 與原 Token 不同")]
    public void ThenResendUsesDifferentToken()
    {
        _lastAttempt!.Token.Should().NotBe(_firstAttempt!.Token);
    }

    [Then("重送使用的 nonce 與原請求不同")]
    public void ThenResendUsesDifferentNonce()
    {
        _lastAttempt!.Nonce.Should().NotBe(_firstAttempt!.Nonce);
    }

    [Then("以最近一次成功回應的訂單編號查詢，回應 200 且品項為 {string}，數量為 {int}")]
    public async Task ThenQueryLastOrderReturnsContent(string item, int quantity)
    {
        var (status, body) = await QueryOrderAsync(_lastOrderId!);
        status.Should().Be(HttpStatusCode.OK, body);
        var document = ReadJson(body);
        document.GetProperty("item").GetString().Should().Be(item);
        document.GetProperty("quantity").GetInt32().Should().Be(quantity);
    }

    [Then("兩次查詢皆回應 200")]
    public void ThenBothQueriesReturnOk()
    {
        _queryStatuses.Should().HaveCount(2);
        _queryStatuses.Should().OnlyContain(status => status == HttpStatusCode.OK);
    }

    [Then("業務去重紀錄數仍與記錄時相同")]
    public void ThenDedupeCountUnchanged()
    {
        Runtime.Orders.IdempotencyRecordCount.Should().Be(_recordCount);
    }

    [Then("06 單第 {int} 項驗收已勾選")]
    public void ThenIssueCheckboxIsChecked(int index)
    {
        CheckboxAt(index).Should().StartWith("- [x] ");
    }

    [Then("06 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        File.ReadAllText(Path.Combine(RepoRoot, IssueRelativePath)).Should().Contain(expected);
    }

    [Then("建立訂單 API 契約包含 {string}")]
    public void ThenOpenApiContains(string expected)
    {
        File.ReadAllText(Path.Combine(RepoRoot, OpenApiRelativePath)).Should().Contain(expected);
    }


    private static string OrderBody(string reference, string item, int quantity)
        => JsonSerializer.Serialize(new { orderReference = reference, item, quantity });

    private static JsonElement ReadJson(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }

    private static Uri OrdersUri(int port, string path = "/orders") => new($"https://localhost:{port}{path}");

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

    private static string CheckboxAt(int index)
    {
        var checkboxes = File.ReadAllText(Path.Combine(RepoRoot, IssueRelativePath)).Split('\n')
            .Where(line => Regex.IsMatch(line, @"^- \[[ x]\] "))
            .ToList();
        checkboxes.Should().HaveCountGreaterThanOrEqualTo(index);
        return checkboxes[index - 1];
    }

    private async Task<Attempt> NewAttemptAsync(string key, string body)
    {
        var token = await EnsureTokenAsync();
        return new Attempt(token, key, body, SignedHttp.NewNonce(), DateTimeOffset.UtcNow);
    }

    private static Attempt Renewed(Attempt attempt, string? key = null, string? token = null)
        => attempt with
        {
            Key = key ?? attempt.Key,
            Token = token ?? attempt.Token,
            Nonce = SignedHttp.NewNonce(),
            Created = DateTimeOffset.UtcNow,
        };

    private async Task<string> EnsureTokenAsync()
    {
        _token ??= await RequestTokenAsync();
        return _token;
    }

    private async Task SendLastAsync(Attempt attempt)
    {
        _lastAttempt = attempt;
        _firstAttempt ??= attempt;
        var result = await SendAsync(attempt, Runtime.OrdersApi.Port);
        _last = result;
        if (result.Status == HttpStatusCode.Created)
        {
            _lastOrderId = ReadJson(result.Body).GetProperty("orderId").GetString();
            _firstOrderId ??= _lastOrderId;
        }
    }

    /// <summary>以目前參數簽署並送出；送往第二個執行個體時沿用主要執行個體的公開目標（Host），與 04 單相同。</summary>
    private static async Task<ApiResponse> SendAsync(Attempt attempt, int port)
    {
        var primaryPort = Runtime.OrdersApi.Port;
        using var request = await SignedHttp.BuildSignedAsync(
            HttpMethod.Post,
            OrdersUri(primaryPort),
            attempt.Token,
            Runtime.SigningKey(ClientId),
            attempt.Body,
            attempt.Key,
            attempt.Created,
            nonce: attempt.Nonce);

        if (port != primaryPort)
        {
            request.RequestUri = OrdersUri(port);
            request.Headers.Host = $"localhost:{primaryPort}";
        }

        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(ClientId));
        return await SignedHttp.SendAsync(client, request);
    }

    private static async Task<ApiResponse> QueryOrderAsync(string orderId)
    {
        var token = await RequestTokenAsync();
        using var request = await SignedHttp.BuildSignedAsync(HttpMethod.Get, OrdersUri(Runtime.OrdersApi.Port, $"/orders/{orderId}"), token, Runtime.SigningKey(ClientId));
        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(ClientId));
        return await SignedHttp.SendAsync(client, request);
    }

    private static async Task<string> RequestTokenAsync()
    {
        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(ClientId));
        using var response = await client.PostAsync(
            new Uri(Runtime.AuthServer.Issuer, "connect/token"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = ClientId,
            }));

        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(body);
        return ReadJson(body).GetProperty("access_token").GetString()!;
    }
}
