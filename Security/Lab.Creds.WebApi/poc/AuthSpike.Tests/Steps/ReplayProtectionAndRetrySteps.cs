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

/// <summary>04 單：防重放（時間窗、nonce 保存、跨執行個體併發）與合法重簽重試的實際呼叫情境。</summary>
[Binding]
public sealed class ReplayProtectionAndRetrySteps
{
    /// <summary>每次取用都產生新的業務識別（orderReference），使各 Scenario 建立的訂單互不去重（06 單）。</summary>
    private static string OrderJson() => $$"""{"orderReference":"{{Guid.NewGuid():N}}","item":"book","quantity":1}""";
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/04-replay-protection-and-retry.md";

    private readonly Dictionary<string, string> _tokens = new();
    private HttpRequestMessage? _originalTemplate;
    private HttpRequestMessage? _latestTemplate;
    private string _latestClientId = string.Empty;
    private HttpStatusCode? _firstStatus;
    private HttpStatusCode? _status;
    private string _responseBody = string.Empty;
    private List<HttpStatusCode> _concurrentStatuses = [];

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    [Given("第二個建立訂單 API 執行個體已啟動並共用防重放儲存")]
    public async Task GivenSecondaryInstanceStarted()
    {
        await Runtime.StartSecondaryOrdersApiAsync(SpikeEnvironment.FreeTcpPort());
    }

    [Given("防重放儲存暫時無法讀寫")]
    public void GivenReplayStoreIsDown()
    {
        Runtime.ReplayStore.SimulateOutage = true;
    }

    [When("簽章呼叫端以 {string} 送出時間參數為 {string} 的建立訂單請求")]
    public async Task WhenOrderSentWithTimeParameters(string clientId, string condition)
    {
        var now = DateTimeOffset.UtcNow;
        var nonce = SignedHttp.NewNonce();
        var (created, expires, dropCreated) = condition switch
        {
            "created 超前 25 秒" => (now.AddSeconds(25), now.AddSeconds(85), false),
            "created 超前 35 秒" => (now.AddSeconds(35), now.AddSeconds(95), false),
            "有效期 60 秒" => (now, now.AddSeconds(60), false),
            "有效期 61 秒" => (now, now.AddSeconds(61), false),
            "已過期" => (now.AddSeconds(-120), now.AddSeconds(-60), false),
            "nonce 為空" => (now, now.AddSeconds(60), false),
            "缺少 created 參數" => (now, now.AddSeconds(60), true),
            _ => throw new ArgumentOutOfRangeException(nameof(condition), condition, "未知的時間參數條件"),
        };

        if (condition == "nonce 為空")
        {
            nonce = string.Empty;
        }

        var template = await BuildSignedCreateAsync(clientId, Guid.NewGuid().ToString(), created, expires, nonce);
        if (dropCreated)
        {
            var input = template.Headers.GetValues("Signature-Input").Single();
            template.Headers.Remove("Signature-Input");
            template.Headers.TryAddWithoutValidation("Signature-Input", Regex.Replace(input, @";created=\d+", string.Empty));
        }

        await SendTemplateAsync(clientId, template, Runtime.OrdersApi.Port);
    }

    [When("簽章呼叫端以 {string} 送出已簽章的建立訂單請求，Idempotency-Key 為 {string}")]
    public async Task WhenSignedOrderSent(string clientId, string idempotencyKey)
    {
        _originalTemplate = await BuildSignedNowAsync(clientId, idempotencyKey);
        await SendTemplateAsync(clientId, _originalTemplate, Runtime.OrdersApi.Port);
        _firstStatus = _status;
    }

    [When("同一份已簽章請求再次送出到主要執行個體")]
    public async Task WhenSameSignedRequestResentToPrimary()
    {
        await SendTemplateAsync(_latestClientId, _latestTemplate!, Runtime.OrdersApi.Port);
    }

    [When("同一份已簽章請求於第二個執行個體重送")]
    public async Task WhenSameSignedRequestResentToSecondary()
    {
        await SendTemplateAsync(_latestClientId, _latestTemplate!, Runtime.SecondaryOrdersApi!.Port);
    }

    [When("簽章呼叫端以 {string} 併發送出同一份已簽章的建立訂單請求 {int} 次到主要執行個體")]
    public async Task WhenSameSignedRequestRacesOnPrimary(string clientId, int count)
    {
        var template = await BuildSignedNowAsync(clientId, Guid.NewGuid().ToString());
        var ports = Enumerable.Repeat(Runtime.OrdersApi.Port, count).ToArray();
        await RaceAsync(clientId, template, ports);
    }

    [When("簽章呼叫端以 {string} 併發送出同一份已簽章的建立訂單請求 {int} 次，交替送往兩個執行個體")]
    public async Task WhenSameSignedRequestRacesAcrossInstances(string clientId, int count)
    {
        var template = await BuildSignedNowAsync(clientId, Guid.NewGuid().ToString());
        var ports = Enumerable.Range(0, count)
            .Select(index => index % 2 == 0 ? Runtime.OrdersApi.Port : Runtime.SecondaryOrdersApi!.Port)
            .ToArray();
        await RaceAsync(clientId, template, ports);
    }

    [When("簽章呼叫端 {string} 以相同 Idempotency-Key {string} 與新 nonce 重新簽署並重試")]
    public async Task WhenCallerRetriesWithNewNonce(string clientId, string idempotencyKey)
    {
        var body = await _latestTemplate!.Content!.ReadAsStringAsync();
        var now = DateTimeOffset.UtcNow;
        _latestTemplate = await BuildSignedCreateAsync(clientId, idempotencyKey, now, now.AddSeconds(60), SignedHttp.NewNonce(), body);
        _latestClientId = clientId;
        await SendTemplateAsync(clientId, _latestTemplate, Runtime.OrdersApi.Port);
    }

    [When("簽章呼叫端以 {string} 送出已簽章的建立訂單請求，Idempotency-Key 為 {string}，並以主要執行個體為目標")]
    public Task WhenSignedOrderSentToPrimary(string clientId, string idempotencyKey)
        => WhenSignedOrderSent(clientId, idempotencyKey);

    [Then("最近一次請求回應 {int}")]
    public void ThenLastRequestReturns(int statusCode)
    {
        _status.Should().Be((HttpStatusCode)statusCode, _responseBody);
    }

    [Then("最近一次請求的錯誤代碼為 {string}")]
    public void ThenLastErrorCodeIs(string code)
    {
        using var document = JsonDocument.Parse(_responseBody);
        document.RootElement.GetProperty("error").GetString().Should().Be(code);
    }

    [Then("第一次送出的回應為 {int}")]
    public void ThenFirstRequestReturns(int statusCode)
    {
        _firstStatus.Should().Be((HttpStatusCode)statusCode);
    }

    [Then("防重放紀錄保存至該請求有效期加 {int} 秒")]
    public void ThenReplayRecordRetainedUntilExpiryPlusTolerance(int seconds)
    {
        var nonce = ParameterOf(_latestTemplate!, "nonce");
        var expires = long.Parse(ParameterOf(_latestTemplate!, "expires"));
        var retainUntil = Runtime.ReplayStore.RetainUntilOf(_latestClientId, nonce);

        retainUntil.Should().Be(DateTimeOffset.FromUnixTimeSeconds(expires).AddSeconds(seconds));
    }

    [Then("通過的請求數為 {int}")]
    public void ThenAcceptedCountIs(int count)
    {
        _concurrentStatuses.Count(status => status == HttpStatusCode.Created).Should().Be(count);
    }

    [Then("被拒絕的請求回應 {int}")]
    public void ThenRejectedRequestsReturn(int statusCode)
    {
        _concurrentStatuses
            .Where(status => status != HttpStatusCode.Created)
            .Should().OnlyContain(status => status == (HttpStatusCode)statusCode);
    }

    [Then("業務 API 建立訂單數為 {int}")]
    public void ThenPrimaryOrderCountIs(int count)
    {
        Runtime.OrdersApi.OrderCount.Should().Be(count);
    }

    [Then("兩個執行個體建立訂單總數為 {int}")]
    public void ThenTotalOrderCountAcrossInstancesIs(int count)
    {
        (Runtime.OrdersApi.OrderCount + Runtime.SecondaryOrdersApi!.OrderCount).Should().Be(count);
    }

    [Then("重試使用的 nonce 與原請求不同")]
    public void ThenRetryUsesNewNonce()
    {
        ParameterOf(_latestTemplate!, "nonce").Should().NotBe(ParameterOf(_originalTemplate!, "nonce"));
    }

    [Then("04 單第 {int} 項驗收已勾選")]
    public void ThenIssueCheckboxIsChecked(int index)
    {
        CheckboxAt(index).Should().StartWith("- [x] ");
    }

    [Then("04 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    private static Uri OrdersUri(int port) => new($"https://localhost:{port}/orders");

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

    private static string ParameterOf(HttpRequestMessage template, string name)
    {
        var input = template.Headers.GetValues("Signature-Input").Single();
        var match = Regex.Match(input, $@";{name}=""?([^"";]*)""?");
        match.Success.Should().BeTrue($"Signature-Input 應包含 {name}");
        return match.Groups[1].Value;
    }

    private async Task<string> TokenAsync(string clientId)
    {
        if (_tokens.TryGetValue(clientId, out var cached))
        {
            return cached;
        }

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
        using var document = JsonDocument.Parse(body);
        var token = document.RootElement.GetProperty("access_token").GetString()!;
        _tokens[clientId] = token;
        return token;
    }

    private async Task<HttpRequestMessage> BuildSignedCreateAsync(
        string clientId,
        string idempotencyKey,
        DateTimeOffset created,
        DateTimeOffset expires,
        string nonce,
        string? body = null)
    {
        return await SignedHttp.BuildSignedAsync(
            HttpMethod.Post,
            OrdersUri(Runtime.OrdersApi.Port),
            await TokenAsync(clientId),
            Runtime.SigningKey(clientId),
            body ?? OrderJson(),
            idempotencyKey,
            created,
            expires,
            nonce);
    }

    private Task<HttpRequestMessage> BuildSignedNowAsync(string clientId, string idempotencyKey)
    {
        var now = DateTimeOffset.UtcNow;
        return BuildSignedCreateAsync(clientId, idempotencyKey, now, now.AddSeconds(60), SignedHttp.NewNonce());
    }

    private async Task RaceAsync(string clientId, HttpRequestMessage template, int[] ports)
    {
        _latestTemplate = template;
        _latestClientId = clientId;
        var results = await Task.WhenAll(ports.Select(port => SendOnceAsync(clientId, template, port)));
        _concurrentStatuses = results.Select(result => result.Status).ToList();
    }

    private async Task SendTemplateAsync(string clientId, HttpRequestMessage template, int port)
    {
        _latestTemplate = template;
        _latestClientId = clientId;
        var response = await SendOnceAsync(clientId, template, port);
        _status = response.Status;
        _responseBody = response.Body;
    }

    /// <summary>以新連線送出；port 與主要執行個體不同時，沿用主要執行個體的公開目標（Host），模擬經同一入口的多個執行個體。</summary>
    private static async Task<ApiResponse> SendOnceAsync(string clientId, HttpRequestMessage template, int port)
    {
        using var request = await SignedHttp.CloneAsync(template, port, Runtime.OrdersApi.Port);
        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(clientId));
        return await SignedHttp.SendAsync(client, request);
    }
}
