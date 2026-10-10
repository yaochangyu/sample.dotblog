using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthSpike.Certificates;
using AuthSpike.Hosting;
using AuthSpike.Signing;
using AuthSpike.Tests.Support;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>
/// 15 單：管理操作稽核。管理操作與稽核讀取皆經真實 mTLS 與 HTTP 路徑（稽核紀錄以管理員身分經管理端點查詢），
/// 不直接讀取稽核紀錄的內部儲存。寫入失敗以執行環境的故障開關模擬。
/// </summary>
[Binding]
public sealed class AdminOperationAuditSteps
{
    private const string ClientId = "orders-client";
    private const string AuditRecordsPath = "admin/audit-records";
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/15-admin-operation-audit.md";

    private X509Certificate2? _pendingCertificate;
    private string? _pendingCertificateRequestPath;
    private SignatureKey? _pendingKey;
    private string? _pendingKeyRequestPath;
    private ApiResponse? _lastResponse;
    private string? _baseline;
    private string? _token;
    private int _callerAuditCount;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

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

    /// <summary>情境中使用的呼叫身分；null 表示不出示用戶端憑證。</summary>
    private static X509Certificate2? Identity(string identity) => identity switch
    {
        "無憑證" => null,
        "未登錄憑證" => Runtime.UnregisteredSelfSignedCertificate,
        "Client 憑證" => Runtime.ClientCertificate(ClientId),
        "管理員憑證" => Runtime.AdministratorCertificate,
        _ => throw new ArgumentOutOfRangeException(nameof(identity), identity, "未知的呼叫身分"),
    };

    private string PendingCertificateRequestPath => _pendingCertificateRequestPath ?? throw new InvalidOperationException("情境應先提交待核准的憑證登錄申請");

    private string PendingKeyRequestPath => _pendingKeyRequestPath ?? throw new InvalidOperationException("情境應先提交待核准的簽章金鑰登錄申請");

    /// <summary>指紋標籤對應：憑證為 SHA-1 Thumbprint；簽章金鑰為公開金鑰 SubjectPublicKeyInfo 的 SHA-256。</summary>
    private string FingerprintOf(string label) => label switch
    {
        "原始憑證" => Runtime.ClientCertificate(ClientId).Thumbprint,
        "待核准憑證" => (_pendingCertificate ?? throw new InvalidOperationException("情境應先提交待核准的憑證登錄申請")).Thumbprint,
        "原始簽章金鑰" => KeyFingerprint(Runtime.SigningKey(ClientId).Key),
        "待核准簽章金鑰" => KeyFingerprint((_pendingKey ?? throw new InvalidOperationException("情境應先提交待核准的簽章金鑰登錄申請")).Key),
        _ => throw new ArgumentOutOfRangeException(nameof(label), label, "未知的指紋標籤"),
    };

    private static string KeyFingerprint(ECDsa key) => Convert.ToHexString(SHA256.HashData(key.ExportSubjectPublicKeyInfo()));

    [Given("為 {string} 提交待核准的憑證登錄申請")]
    public async Task GivenPendingCertificateSubmitted(string clientId)
    {
        _pendingCertificate = SpikeCertificates.CreateSelfSignedClientCertificate($"{clientId}-pending-{Runtime.EnvironmentName}-{Guid.NewGuid():N}");
        var body = JsonSerializer.Serialize(new { clientId, publicCertificatePem = _pendingCertificate.ExportCertificatePem() });
        var response = await PostJsonAsync("client-certificate-requests", body);
        response.Status.Should().Be(HttpStatusCode.Accepted, response.Body);
        _pendingCertificateRequestPath = $"admin/client-certificate-requests/{RequestIdOf(response.Body)}";
    }

    [Given("為 {string} 提交待核准的簽章金鑰登錄申請")]
    public async Task GivenPendingSigningKeySubmitted(string clientId)
    {
        _pendingKey = Runtime.CreateSigningKey(clientId);
        var body = JsonSerializer.Serialize(new
        {
            clientId,
            keyId = _pendingKey.KeyId,
            publicKeyPem = _pendingKey.Key.ExportSubjectPublicKeyInfoPem(),
        });
        var response = await PostJsonAsync("signing-key-requests", body);
        response.Status.Should().Be(HttpStatusCode.Accepted, response.Body);
        _pendingKeyRequestPath = $"admin/signing-key-requests/{RequestIdOf(response.Body)}";
    }

    [Given("記錄稽核情境的信任名單基準")]
    public async Task GivenRecordsBaseline()
    {
        _baseline = await AdministratorInterfaceSteps.TrustListBodyAsync();
    }

    [Given("管理操作稽核暫時無法寫入")]
    public void GivenAdministrativeAuditWritesFail()
    {
        Runtime.AdministrativeAuditLog.SimulateWriteFailure = true;
    }

    [Given("稽核情境中 {string} 以原始憑證取得 Token")]
    public async Task GivenClientObtainsToken(string clientId)
    {
        var (issued, token) = await AdministratorInterfaceSteps.RequestTokenAsync(Runtime.ClientCertificate(clientId), clientId);
        issued.Should().BeTrue("Client 憑證應能取得 Token");
        _token = token;
    }

    [Given("記錄呼叫者安全稽核紀錄數")]
    public void GivenRecordsCallerAuditCount()
    {
        _callerAuditCount = Runtime.AuditLog.Snapshot().Count;
    }

    [When("稽核情境中以 {string} 身分核准待核准的憑證申請")]
    public async Task WhenIdentityApprovesPendingCertificate(string identity)
    {
        _lastResponse = await AdministratorInterfaceSteps.SendAdminAsync(Identity(identity), HttpMethod.Post, $"{PendingCertificateRequestPath}/approve");
    }

    [When("稽核情境中以 {string} 身分拒絕待核准的憑證申請")]
    public async Task WhenIdentityRejectsPendingCertificate(string identity)
    {
        _lastResponse = await AdministratorInterfaceSteps.SendAdminAsync(Identity(identity), HttpMethod.Post, $"{PendingCertificateRequestPath}/reject");
    }

    [When("稽核情境中以 {string} 身分核准待核准的簽章金鑰申請")]
    public async Task WhenIdentityApprovesPendingSigningKey(string identity)
    {
        _lastResponse = await AdministratorInterfaceSteps.SendAdminAsync(Identity(identity), HttpMethod.Post, $"{PendingKeyRequestPath}/approve");
    }

    [When("稽核情境中以 {string} 身分拒絕待核准的簽章金鑰申請")]
    public async Task WhenIdentityRejectsPendingSigningKey(string identity)
    {
        _lastResponse = await AdministratorInterfaceSteps.SendAdminAsync(Identity(identity), HttpMethod.Post, $"{PendingKeyRequestPath}/reject");
    }

    [When("稽核情境中以 {string} 身分停用 {string}")]
    public async Task WhenIdentityDisablesClient(string identity, string clientId)
    {
        _lastResponse = await AdministratorInterfaceSteps.SendAdminAsync(Identity(identity), HttpMethod.Post, $"admin/clients/{clientId}/disable");
    }

    [When("稽核情境中以 {string} 身分退役 {string} 的原始憑證")]
    public async Task WhenIdentityRetiresCertificate(string identity, string clientId)
    {
        _lastResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            Identity(identity),
            HttpMethod.Post,
            $"admin/clients/{clientId}/certificates/{Runtime.ClientCertificate(clientId).Thumbprint}/retire");
    }

    [When("稽核情境中以 {string} 身分撤銷 {string} 的原始憑證")]
    public async Task WhenIdentityRevokesCertificate(string identity, string clientId)
    {
        _lastResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            Identity(identity),
            HttpMethod.Post,
            $"admin/clients/{clientId}/certificates/{Runtime.ClientCertificate(clientId).Thumbprint}/revoke");
    }

    [When("稽核情境中以 {string} 身分退役 {string} 的原始簽章金鑰")]
    public async Task WhenIdentityRetiresSigningKey(string identity, string clientId)
    {
        _lastResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            Identity(identity),
            HttpMethod.Post,
            $"admin/clients/{clientId}/signing-keys/{Runtime.SigningKey(clientId).KeyId}/retire");
    }

    [When("稽核情境中以 {string} 身分撤銷 {string} 的原始簽章金鑰")]
    public async Task WhenIdentityRevokesSigningKey(string identity, string clientId)
    {
        _lastResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            Identity(identity),
            HttpMethod.Post,
            $"admin/clients/{clientId}/signing-keys/{Runtime.SigningKey(clientId).KeyId}/revoke");
    }

    [Then("稽核情境的管理操作回應為 {int}")]
    public void ThenAdministrativeResponseIs(int expected)
    {
        _lastResponse.Should().NotBeNull("情境應先送出管理操作");
        ((int)_lastResponse!.Status).Should().Be(expected, _lastResponse.Body);
    }

    [Then("管理操作稽核包含 {string} 紀錄，對象 Client 為 {string}，操作者為 {string}，結果為 {string}")]
    public async Task ThenAuditContainsRecord(string operation, string clientId, string actorStatus, string outcome)
    {
        var record = (await AuditRecordsAsync()).LastOrDefault(candidate => candidate.Operation == operation);
        record.Should().NotBeNull($"應有 {operation} 紀錄");
        record!.ClientId.Should().Be(clientId);
        record.ActorStatus.Should().Be(actorStatus);
        record.Outcome.Should().Be(outcome);
        record.CorrelationId.Should().NotBeNullOrEmpty();
        DateTimeOffset.TryParse(record.OccurredAt, out _).Should().BeTrue("紀錄應含可解析的時間");
        if (actorStatus == "已驗證管理員")
        {
            record.ActorThumbprint.Should().Be(Runtime.AdministratorCertificate.Thumbprint, "已驗證管理員的紀錄應標示管理員憑證指紋");
        }
    }

    [Then("管理操作稽核的 {string} 紀錄對象指紋為 {string}")]
    public async Task ThenAuditFingerprintIs(string operation, string label)
    {
        var record = (await AuditRecordsAsync()).LastOrDefault(candidate => candidate.Operation == operation);
        record.Should().NotBeNull($"應有 {operation} 紀錄");
        record!.Fingerprint.Should().Be(FingerprintOf(label));
    }

    [Then("稽核情境的待核准申請狀態為 {string}")]
    public async Task ThenPendingCertificateRequestStatusIs(string expected)
    {
        var response = await AdministratorInterfaceSteps.SendAdminAsync(Runtime.AdministratorCertificate, HttpMethod.Get, PendingCertificateRequestPath);
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        using var document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("status").GetString().Should().Be(expected);
    }

    [Then("稽核情境的待核准憑證無法取得 Token")]
    public async Task ThenPendingCertificateCannotGetToken()
    {
        var (issued, _) = await AdministratorInterfaceSteps.RequestTokenAsync(_pendingCertificate, ClientId);
        issued.Should().BeFalse();
    }

    [Then("管理操作稽核不含私鑰、原始 Token 或憑證私有內容")]
    public async Task ThenAuditHasNoSecrets()
    {
        var body = await AuditBodyAsync();
        // 非空斷言：確認紀錄確實存在，避免空清單讓「不含」成立。
        body.Should().Contain("signing_key.approve");
        body.Should().NotContain("PRIVATE KEY");
        if (_token is not null)
        {
            body.Should().NotContain(_token);
        }

        var privateScalar = Runtime.SigningKey(ClientId).Key.ExportParameters(includePrivateParameters: true).D!;
        body.Should().NotContain(Convert.ToBase64String(privateScalar));
    }

    [Then("信任名單與稽核情境基準相同")]
    public async Task ThenTrustListUnchanged()
    {
        _baseline.Should().NotBeNull("情境應先記錄信任名單基準");
        (await AdministratorInterfaceSteps.TrustListBodyAsync()).Should().Be(_baseline);
    }

    [Then("稽核情境中 {string} 的原始憑證仍可取得 Token")]
    public async Task ThenOriginalCertificateStillIssuesToken(string clientId)
    {
        var (issued, _) = await AdministratorInterfaceSteps.RequestTokenAsync(Runtime.ClientCertificate(clientId), clientId);
        issued.Should().BeTrue("寫入失敗時原始憑證應維持有效");
    }

    [Then("呼叫者安全稽核紀錄數未改變")]
    public void ThenCallerAuditCountUnchanged()
    {
        Runtime.AuditLog.Snapshot().Count.Should().Be(_callerAuditCount);
    }

    [Then("管理操作稽核與呼叫者安全稽核為不同的紀錄集合")]
    public void ThenAdministrativeAuditIsSeparateFromCallerAudit()
    {
        ((object)Runtime.AdministrativeAuditLog).Should().NotBeSameAs(Runtime.AuditLog);
    }

    [Then("15 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    [Then("15 單第 {int} 項驗收已勾選")]
    public void ThenAcceptanceItemChecked(int index)
    {
        var boxes = Regex.Matches(IssueText, @"^- \[( |x)\] ", RegexOptions.Multiline);
        boxes.Count.Should().BeGreaterThanOrEqualTo(index);
        boxes[index - 1].Groups[1].Value.Should().Be("x");
    }

    private static async Task<ApiResponse> PostJsonAsync(string path, string json)
    {
        using var client = SignedHttp.CreateClient(Runtime, null);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"https://localhost:{Runtime.AuthServer.Port}/{path}"))
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        return await SignedHttp.SendAsync(client, request);
    }

    private static string RequestIdOf(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("requestId").GetString()!;
    }

    /// <summary>以管理員身分取得稽核紀錄原始回應；管理員驗證本身也在此被檢查（必須 200）。</summary>
    private static async Task<string> AuditBodyAsync()
    {
        var response = await AdministratorInterfaceSteps.SendAdminAsync(Runtime.AdministratorCertificate, HttpMethod.Get, AuditRecordsPath);
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        return response.Body;
    }

    private static async Task<List<AuditView>> AuditRecordsAsync()
    {
        using var document = JsonDocument.Parse(await AuditBodyAsync());
        return document.RootElement.EnumerateArray().Select(AuditView.From).ToList();
    }

    private sealed record AuditView(
        string CorrelationId,
        string OccurredAt,
        string Operation,
        string ActorStatus,
        string? ActorThumbprint,
        string? ClientId,
        string? Fingerprint,
        string Outcome)
    {
        public static AuditView From(JsonElement element) => new(
            Text(element, "correlationId") ?? string.Empty,
            Text(element, "occurredAt") ?? string.Empty,
            Text(element, "operation") ?? string.Empty,
            Text(element, "actorStatus") ?? string.Empty,
            Text(element, "actorThumbprint"),
            Text(element, "clientId"),
            Text(element, "fingerprint"),
            Text(element, "outcome") ?? string.Empty);

        private static string? Text(JsonElement element, string name)
        {
            var value = element.GetProperty(name);
            return value.ValueKind == JsonValueKind.Null ? null : value.GetString();
        }
    }
}
