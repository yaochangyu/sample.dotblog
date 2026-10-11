using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthSpike.Certificates;
using AuthSpike.Hosting;
using AuthSpike.Tests.Support;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>
/// 20 單：被拒絕申請嘗試的稽核。所有申請與稽核讀取皆經真實 mTLS 與 HTTP 管理端點，不直接讀取稽核紀錄的內部儲存。
/// </summary>
[Binding]
public sealed class RejectedRequestAuditSteps
{
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/20-rejected-request-audit.md";
    private const string AuditRecordsPath = "admin/audit-records";
    private const string ClientId = "orders-client";
    private const string CertificateSubmit = "certificate.submit";

    private ApiResponse? _response;
    private string? _existingRequestId;
    private string? _existingThumbprint;
    private string? _token;

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

    /// <summary>呼叫身分：「無憑證」、「未登錄憑證」，或某 Client 目前登錄的 mTLS 憑證。</summary>
    private static X509Certificate2? Identity(string identity) => identity switch
    {
        "無憑證" => null,
        "未登錄憑證" => Runtime.UnregisteredSelfSignedCertificate,
        _ => Runtime.ClientCertificate(identity),
    };

    [Given("20 單情境中為 {string} 已有待核准的憑證登錄申請")]
    public async Task GivenPendingCertificateRequest(string clientId)
    {
        var candidate = SpikeCertificates.CreateSelfSignedClientCertificate($"rejected-audit-existing-{Runtime.EnvironmentName}-{Guid.NewGuid():N}");
        var response = await ApplicantProofs.SubmitCertificateAsync(
            Runtime,
            Runtime.ClientCertificate(clientId),
            clientId,
            candidate.ExportCertificatePem(),
            ApplicantProofs.CertificateProof(clientId, candidate));
        response.Status.Should().Be(HttpStatusCode.Accepted, response.Body);
        using var document = JsonDocument.Parse(response.Body);
        _existingRequestId = document.RootElement.GetProperty("requestId").GetString();
        _existingThumbprint = candidate.Thumbprint;
    }

    [Given("20 單情境中 {string} 以原始憑證取得 Token")]
    public async Task GivenClientObtainsToken(string clientId)
    {
        var (issued, token) = await AdministratorInterfaceSteps.RequestTokenAsync(Runtime.ClientCertificate(clientId), clientId);
        issued.Should().BeTrue("Client 憑證應能取得 Token");
        _token = token;
    }

    [When("20 單情境中以 {string} 身分為 {string} 提出持有證明為 {string} 的新憑證登錄申請")]
    public async Task WhenSubmitsCertificateRequest(string identity, string claimedClientId, string proofMode)
    {
        var candidate = SpikeCertificates.CreateSelfSignedClientCertificate($"rejected-audit-{Runtime.EnvironmentName}-{Guid.NewGuid():N}");
        _response = await SubmitAsync(identity, claimedClientId, proofMode, candidate);
    }

    [When("20 單情境的管理操作稽核恢復可寫入")]
    public void GivenAuditWritesRecover()
    {
        Runtime.AdministrativeAuditLog.SimulateWriteFailure = false;
    }

    [Then("20 單情境中連續 {int} 筆有效的新憑證登錄申請皆被受理")]
    public async Task ThenConsecutiveValidSubmissionsAccepted(int count)
    {
        // 503 的申請不得佔用待核准額度：之後連續申請數等於上限時仍全部受理，代表失敗的申請未被建立。
        for (var i = 1; i <= count; i++)
        {
            var candidate = SpikeCertificates.CreateSelfSignedClientCertificate($"rejected-audit-recover-{Runtime.EnvironmentName}-{Guid.NewGuid():N}");
            var response = await SubmitAsync(ClientId, ClientId, "有效", candidate);
            response.Status.Should().Be(HttpStatusCode.Accepted, $"第 {i} 筆應被受理：{response.Body}");
        }
    }

    [Then("20 單情境的申請回應為 {int}")]
    public void ThenResponseIs(int expected)
    {
        _response.Should().NotBeNull("情境應先提出申請");
        ((int)_response!.Status).Should().Be(expected, _response.Body);
    }

    [Then("20 單情境的回應錯誤為 {string}")]
    public void ThenErrorIs(string expected)
    {
        _response.Should().NotBeNull("情境應先提出申請");
        using var document = JsonDocument.Parse(_response!.Body);
        document.RootElement.GetProperty("error").GetString().Should().Be(expected);
    }

    [Then("20 單情境的回應不含既有待核准申請的識別或指紋")]
    public void ThenFailureDoesNotLeakExistingRequest()
    {
        _response.Should().NotBeNull("情境應先提出申請");
        _existingRequestId.Should().NotBeNullOrEmpty("情境應先建立既有的待核准申請");
        _existingThumbprint.Should().NotBeNullOrEmpty("情境應先建立既有的待核准申請");
        _response!.Body.Should().NotContain(_existingRequestId!);
        _response.Body.Should().NotContain(_existingThumbprint!);
    }

    [Then("20 單情境最新的申請稽核對象 Client 為 {string}，操作者為 {string}，結果為 {string}，結果代碼為 {int}，原因為 {string}")]
    public async Task ThenLatestSubmitAuditIs(string clientId, string actorStatus, string outcome, int resultStatus, string reason)
    {
        var record = (await AuditRecordsAsync()).LastOrDefault(candidate => candidate.Operation == CertificateSubmit);
        record.Should().NotBeNull("應有 certificate.submit 紀錄");
        record!.ClientId.Should().Be(clientId);
        record.ActorStatus.Should().Be(actorStatus);
        record.Outcome.Should().Be(outcome);
        record.ResultStatus.Should().Be(resultStatus);
        record.Reason.Should().Be(reason);
    }

    [Then("20 單情境最新的申請稽核的操作者指紋為未登錄憑證")]
    public async Task ThenLatestActorThumbprintIsPresentedCertificate()
    {
        var record = (await AuditRecordsAsync()).LastOrDefault(candidate => candidate.Operation == CertificateSubmit);
        record.Should().NotBeNull("應有 certificate.submit 紀錄");
        record!.ActorThumbprint.Should().Be(Runtime.UnregisteredSelfSignedCertificate.Thumbprint, "紀錄應只標示出示的憑證指紋，不視為身分");
    }

    [Then("20 單情境的管理操作稽核不含私鑰、原始 Token 或憑證私有內容")]
    public async Task ThenAuditHasNoSecrets()
    {
        var body = await AuditBodyAsync();
        // 非空斷言：確認被拒絕的申請確實留下紀錄，避免空清單讓「不含」成立。
        body.Should().Contain(CertificateSubmit);
        body.Should().Contain("\"rejected\"");
        body.Should().NotContain("PRIVATE KEY");
        _token.Should().NotBeNullOrEmpty("情境應先取得原始 Token，才能驗證稽核不含該 Token");
        body.Should().NotContain(_token!);

        var privateScalar = Runtime.SigningKey(ClientId).Key.ExportParameters(includePrivateParameters: true).D!;
        body.Should().NotContain(Convert.ToBase64String(privateScalar));
    }

    [Then("20 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    [Then("20 單第 {int} 項驗收已勾選")]
    public void ThenAcceptanceItemChecked(int index)
    {
        var boxes = Regex.Matches(IssueText, @"^- \[( |x)\] ", RegexOptions.Multiline);
        boxes.Count.Should().BeGreaterThanOrEqualTo(index);
        boxes[index - 1].Groups[1].Value.Should().Be("x");
    }

    private static Task<ApiResponse> SubmitAsync(string identity, string claimedClientId, string proofMode, X509Certificate2 candidate)
    {
        var proof = proofMode switch
        {
            "有效" => ApplicantProofs.CertificateProof(claimedClientId, candidate),
            "缺漏" => null,
            "他人私鑰" => ApplicantProofs.ForeignCertificateProof(claimedClientId, candidate, Runtime.UnregisteredSelfSignedCertificate),
            _ => throw new ArgumentOutOfRangeException(nameof(proofMode), proofMode, "未知的持有證明模式"),
        };
        return ApplicantProofs.SubmitCertificateAsync(Runtime, Identity(identity), claimedClientId, candidate.ExportCertificatePem(), proof);
    }

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
        string Operation,
        string ActorStatus,
        string? ActorThumbprint,
        string? ClientId,
        string Outcome,
        string Reason,
        int ResultStatus)
    {
        public static AuditView From(JsonElement element) => new(
            Text(element, "operation") ?? string.Empty,
            Text(element, "actorStatus") ?? string.Empty,
            Text(element, "actorThumbprint"),
            Text(element, "clientId"),
            Text(element, "outcome") ?? string.Empty,
            Text(element, "reason") ?? string.Empty,
            element.GetProperty("resultStatus").GetInt32());

        private static string? Text(JsonElement element, string name)
            => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
