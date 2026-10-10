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
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>09 單：安全追查已驗證呼叫者（稽核紀錄的內容、關聯、禁止資料與寫入失敗行為）。</summary>
[Binding]
public sealed class VerifiedCallerAuditSteps
{
    private const string DefaultItem = "book";
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/09-verified-caller-audit.md";

    private string? _token;
    private string _tokenClientId = string.Empty;
    private string? _correlationId;
    private HttpStatusCode? _status;
    private string _responseBody = string.Empty;
    private string _responseHeaders = string.Empty;
    private string? _errorCode;
    private string? _lastSignature;
    private string? _lastSignatureInput;
    private HttpRequestMessage? _lastRequest;
    private readonly Dictionary<string, string> _correlationIds = new();

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    private static Uri OrdersUri(string path = "/orders")
        => new($"https://localhost:{Runtime.OrdersApi.Port}{path}");

    [Given("稽核呼叫端 {string} 已以 mTLS 取得 Token")]
    public async Task GivenAuditCallerHasToken(string clientId)
    {
        _tokenClientId = clientId;
        _token = await RequestTokenAsync(clientId);
        _token.Should().NotBeNullOrEmpty();
    }

    [Given("稽核紀錄暫時無法寫入")]
    public void GivenAuditWritesFail()
    {
        Runtime.AuditLog.SimulateWriteFailure = true;
    }

    [When("稽核呼叫端以 {string} 送出已簽章的建立訂單請求")]
    public async Task WhenAuditCallerCreatesSignedOrder(string clientId)
    {
        var request = BuildCreateRequest(DefaultItem);
        await SignAsync(request, clientId);
        await SendAsync(request);
    }

    [When("稽核呼叫端以 {string} 送出已簽章的建立訂單請求，品項為 {string}")]
    public async Task WhenAuditCallerCreatesSignedOrderWithItem(string clientId, string item)
    {
        var request = BuildCreateRequest(item);
        await SignAsync(request, clientId);
        await SendAsync(request);
    }

    [When("稽核呼叫端以 {string} 未簽章送出建立訂單請求，並附帶 X-Client-Id 標頭 {string}")]
    public async Task WhenAuditCallerCreatesUnsignedOrder(string clientId, string presentedClientId)
    {
        var request = BuildCreateRequest(DefaultItem);
        request.Headers.TryAddWithoutValidation("X-Client-Id", presentedClientId);
        await SendAsync(request);
    }

    [When("稽核呼叫端以 {string} 送出已簽章的建立訂單請求，並改動 Body")]
    public async Task WhenAuditCallerTampersBody(string clientId)
    {
        var request = BuildCreateRequest(DefaultItem);
        await SignAsync(request, clientId);
        var contentType = request.Content!.Headers.GetValues("Content-Type").Single();
        var digest = request.Content.Headers.GetValues("Content-Digest").Single();
        request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes("""{"item":"book","quantity":9}"""));
        request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        request.Content.Headers.TryAddWithoutValidation("Content-Digest", digest);
        await SendAsync(request);
    }

    [When("稽核呼叫端以 {string} 送出已簽章的建立訂單請求，但以 {string} 的簽章金鑰簽署")]
    public async Task WhenAuditCallerSignsWithOtherKey(string clientId, string keyOwner)
    {
        var request = BuildCreateRequest(DefaultItem);
        var now = DateTimeOffset.UtcNow;
        await BusinessRequestSigner.SignAsync(request, Runtime.SigningKey(keyOwner), now, now.AddSeconds(60), NewNonce());
        await SendAsync(request);
    }

    [When("稽核呼叫端重送同一份已簽章的建立訂單請求")]
    public async Task WhenAuditCallerResendsSignedOrder()
    {
        _lastRequest.Should().NotBeNull("必須先送出已簽章請求");
        var original = _lastRequest!;
        var body = await original.Content!.ReadAsByteArrayAsync();
        var resent = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Content = new ByteArrayContent(body),
        };
        foreach (var header in original.Headers)
        {
            resent.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var header in original.Content.Headers)
        {
            resent.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        await SendAsync(resent);
    }

    [When("稽核呼叫端持無效 Token 送出建立訂單請求，並附帶 X-Client-Id 標頭 {string}")]
    public async Task WhenAuditCallerUsesInvalidToken(string presentedClientId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, OrdersUri())
        {
            Content = new StringContent("""{"item":"book","quantity":1}""", Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token-value");
        request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString());
        request.Headers.TryAddWithoutValidation("X-Client-Id", presentedClientId);
        await SendAsync(request);
    }

    [When("稽核呼叫端以 {string} 持無 orders 授權範圍的 Token 送出建立訂單請求")]
    public async Task WhenAuditCallerWithoutScopeCreatesOrder(string clientId)
    {
        _tokenClientId = clientId;
        _token = await RequestTokenAsync(clientId);
        var request = BuildCreateRequest(DefaultItem);
        await SendAsync(request);
    }

    [When("稽核呼叫端把最近一次請求的關聯識別記為 {string}")]
    public void WhenAuditCallerNamesCorrelation(string name)
    {
        _correlationId.Should().NotBeNullOrEmpty();
        _correlationIds[name] = _correlationId!;
    }

    [Then("稽核呼叫端最近一次請求回應 {int}")]
    public void ThenLastRequestReturns(int statusCode)
    {
        _status.Should().Be((HttpStatusCode)statusCode, _responseBody);
    }

    [Then("稽核呼叫端最近一次請求的錯誤代碼為 {string}")]
    public void ThenLastRequestErrorCodeIs(string errorCode)
    {
        _errorCode.Should().Be(errorCode, _responseBody);
    }

    [Then("最近一次請求的稽核紀錄結果為 {string}，原因為 {string}")]
    public void ThenLastRecordOutcomeAndReason(string outcome, string reason)
    {
        var record = LastRecord();
        record.Outcome.Should().Be(outcome);
        record.Reason.Should().Be(reason);
    }

    [Then("最近一次請求的稽核紀錄已驗證 Client 為 {string}")]
    public void ThenLastRecordVerifiedClientIs(string clientId)
    {
        LastRecord().VerifiedClientId.Should().Be(clientId);
    }

    [Then("最近一次請求的稽核紀錄沒有已驗證 Client")]
    public void ThenLastRecordHasNoVerifiedClient()
    {
        LastRecord().VerifiedClientId.Should().BeNull();
    }

    [Then("最近一次請求的稽核紀錄簽章金鑰識別為 {string}")]
    public void ThenLastRecordSignatureKeyIs(string keyId)
    {
        LastRecord().SignatureKeyId.Should().Be(keyId);
    }

    [Then("最近一次請求的稽核紀錄操作為 {string}")]
    public void ThenLastRecordOperationIs(string operation)
    {
        LastRecord().Operation.Should().Be(operation);
    }

    [Then("最近一次請求的稽核紀錄憑證指紋與 {string} 用戶端憑證一致")]
    public void ThenLastRecordThumbprintMatches(string clientId)
    {
        LastRecord().CertificateThumbprint.Should().Be(Runtime.ClientCertificate(clientId).Thumbprint);
    }

    [Then("最近一次請求的稽核紀錄的關聯識別與回應 X-Correlation-Id 相同")]
    public void ThenLastRecordCorrelationMatchesHeader()
    {
        _correlationId.Should().NotBeNullOrEmpty();
        LastRecord().CorrelationId.Should().Be(_correlationId);
    }

    [Then("最近一次請求的稽核紀錄未驗證的 Token Client 為 {string}")]
    public void ThenLastRecordUnverifiedTokenClientIs(string clientId)
    {
        LastRecord().UnverifiedTokenClientId.Should().Be(clientId);
    }

    [Then("最近一次請求的稽核紀錄未驗證的 X-Client-Id 宣稱為 {string}")]
    public void ThenLastRecordPresentedClientIdIs(string clientId)
    {
        LastRecord().PresentedClientIdHeader.Should().Be(clientId);
    }

    [Then("依關聯識別 {string} 的稽核紀錄結果為 {string}，已驗證 Client 為 {string}")]
    public void ThenNamedRecordAcceptedAs(string name, string outcome, string clientId)
    {
        var record = NamedRecord(name);
        record.Outcome.Should().Be(outcome);
        record.VerifiedClientId.Should().Be(clientId);
    }

    [Then("依關聯識別 {string} 的稽核紀錄結果為 {string}，已驗證 Client 為空")]
    public void ThenNamedRecordHasNoVerifiedClient(string name, string outcome)
    {
        var record = NamedRecord(name);
        record.Outcome.Should().Be(outcome);
        record.VerifiedClientId.Should().BeNull();
    }

    [Then("稽核紀錄全部不含原始 Token")]
    public void ThenAuditDoesNotContainToken()
    {
        _token.Should().NotBeNullOrEmpty();
        SerializedAudit().Should().NotContain(_token!);
    }

    [Then("稽核紀錄全部不含 {string}")]
    public void ThenAuditDoesNotContainText(string text)
    {
        SerializedAudit().Should().NotContain(text);
    }

    [Then("稽核紀錄全部不含 {string} 的簽章私鑰")]
    public void ThenAuditDoesNotContainSigningKey(string clientId)
    {
        var parameters = Runtime.SigningKey(clientId).Key.ExportParameters(includePrivateParameters: true);
        var audit = SerializedAudit();
        audit.Should().NotContain(Convert.ToBase64String(parameters.D!));
        audit.Should().NotContain(Convert.ToBase64String(Runtime.SigningKey(clientId).Key.ExportPkcs8PrivateKey()));
    }

    [Then("稽核紀錄全部不含簽章基底與 Signature 標頭值")]
    public void ThenAuditDoesNotContainSignatureMaterial()
    {
        var audit = SerializedAudit();
        audit.Should().NotContain("@signature-params");
        audit.Should().NotContain("\"@method\"");
        if (_lastSignature is not null)
        {
            audit.Should().NotContain(_lastSignature);
        }

        if (_lastSignatureInput is not null)
        {
            audit.Should().NotContain(_lastSignatureInput);
        }
    }

    [Then("最近一次回應不含原始 Token、Signature 或 Signature-Input 標頭值與簽章基底")]
    public void ThenLastResponseLeaksNothing()
    {
        var response = _responseBody + "\n" + _responseHeaders;
        if (_token is not null)
        {
            response.Should().NotContain(_token);
        }

        if (_lastSignature is not null)
        {
            response.Should().NotContain(_lastSignature);
        }

        if (_lastSignatureInput is not null)
        {
            response.Should().NotContain(_lastSignatureInput);
        }

        response.Should().NotContain("@signature-params");
    }

    [Then("稽核紀錄儲存與防重放儲存為不同物件")]
    public void ThenAuditStoreIsSeparateFromReplayStore()
    {
        ((object)Runtime.AuditLog).Should().NotBeSameAs(Runtime.ReplayStore);
    }

    [Then("稽核紀錄保存期為 {int} 天")]
    public void ThenAuditRetentionIsDays(int days)
    {
        SecurityAuditLog.RetentionPeriod.Should().Be(TimeSpan.FromDays(days));
    }

    [Then("稽核紀錄不經業務 API 公開，查詢 {string} 回應 {int}")]
    public async Task ThenAuditIsNotExposedByApi(string path, int statusCode)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, OrdersUri(path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        await SendAsync(request);
        _status.Should().Be((HttpStatusCode)statusCode, _responseBody);
    }

    [Then("稽核情境中業務 API 建立訂單總數為 {int}")]
    public void ThenScenarioOrderCountIs(int count)
    {
        Runtime.Orders.Count.Should().Be(count);
    }

    [Given("09 單的實作紀錄可讀取")]
    public void ThenIssueRecordIsReadable()
    {
        File.Exists(Path.Combine(RepoRoot, IssueRelativePath)).Should().BeTrue();
    }

    [Then("09 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    [Then("09 單第 {int} 項驗收已勾選")]
    public void ThenIssueCheckboxIsChecked(int index)
    {
        CheckboxAt(index).Should().StartWith("- [x] ");
    }

    private SecurityAuditRecord LastRecord()
    {
        _correlationId.Should().NotBeNullOrEmpty("最近一次回應應帶 X-Correlation-Id");
        var records = Runtime.AuditLog.Snapshot().Where(record => record.CorrelationId == _correlationId).ToList();
        records.Should().ContainSingle("每筆請求應恰有一筆稽核紀錄");
        return records[0];
    }

    private SecurityAuditRecord NamedRecord(string name)
    {
        _correlationIds.Should().ContainKey(name);
        var correlationId = _correlationIds[name];
        var records = Runtime.AuditLog.Snapshot().Where(record => record.CorrelationId == correlationId).ToList();
        records.Should().ContainSingle();
        return records[0];
    }

    /// <summary>序列化全部稽核紀錄；要求至少有一筆紀錄，避免空紀錄讓「不含」檢查空轉通過。</summary>
    private string SerializedAudit()
    {
        var records = Runtime.AuditLog.Snapshot();
        records.Should().NotBeEmpty("必須先有稽核紀錄，才能檢查其內容");
        return JsonSerializer.Serialize(records);
    }

    private static string NewNonce() => Guid.NewGuid().ToString("N");

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

    private HttpRequestMessage BuildCreateRequest(string item)
    {
        var json = JsonSerializer.Serialize(new { item, quantity = 1 });
        var request = new HttpRequestMessage(HttpMethod.Post, OrdersUri())
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString());
        return request;
    }

    private static Task SignAsync(HttpRequestMessage request, string clientId)
    {
        var now = DateTimeOffset.UtcNow;
        return BusinessRequestSigner.SignAsync(request, Runtime.SigningKey(clientId), now, now.AddSeconds(60), NewNonce());
    }

    private async Task SendAsync(HttpRequestMessage request)
    {
        _lastRequest = request;
        _lastSignatureInput = request.Headers.TryGetValues("Signature-Input", out var inputs) ? inputs.Single() : null;
        _lastSignature = request.Headers.TryGetValues("Signature", out var signatures) ? signatures.Single() : null;

        using var client = CreateHttpClient(Runtime.ClientCertificate(_tokenClientId));
        using var response = await client.SendAsync(request);
        _status = response.StatusCode;
        _responseBody = await response.Content.ReadAsStringAsync();
        _responseHeaders = string.Join(
            "\n",
            response.Headers.Concat(response.Content.Headers).Select(header => $"{header.Key}: {string.Join(",", header.Value)}"));
        _correlationId = response.Headers.TryGetValues("X-Correlation-Id", out var correlation) ? correlation.Single() : null;
        _errorCode = ReadErrorCode(_responseBody);
    }

    private static string? ReadErrorCode(string body)
    {
        if (string.IsNullOrWhiteSpace(body) || !body.TrimStart().StartsWith('{'))
        {
            return null;
        }

        using var document = JsonDocument.Parse(body);
        return document.RootElement.TryGetProperty("error", out var error) ? error.GetString() : null;
    }

    private static async Task<string> RequestTokenAsync(string clientId)
    {
        using var client = CreateHttpClient(Runtime.ClientCertificate(clientId));
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
        return document.RootElement.GetProperty("access_token").GetString()!;
    }

    /// <summary>每次呼叫都使用新連線，確保 TLS 用戶端憑證依本次呼叫決定。</summary>
    private static HttpClient CreateHttpClient(X509Certificate2 certificate)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = Runtime.Trust.ServerCertificateValidator,
            ClientCertificateOptions = ClientCertificateOption.Manual,
        };
        handler.ClientCertificates.Add(certificate);
        return new HttpClient(handler, disposeHandler: true);
    }
}
