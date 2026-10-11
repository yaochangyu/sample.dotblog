using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using AuthSpike.Certificates;
using AuthSpike.Hosting;
using AuthSpike.Signing;
using AuthSpike.Tests.Support;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>
/// 持有證明的綁定、時間窗與一次性 nonce（18 單後續）。所有呼叫皆經真實 mTLS 與 HTTP 申請端點，稽核以管理端點查詢。
/// </summary>
[Binding]
public sealed class PossessionProofBindingSteps
{
    private const string AuditRecordsPath = "admin/audit-records";
    private const string ProofRejectionReason = "possession_proof_invalid";
    private const string SpecRelativePath = ".scratch/server-to-server-api-protection/spec.md";
    private const string AdrRelativePath = "docs/adr/0001-lab-poc-exceptions.md";

    private X509Certificate2? _candidate;
    private SignatureKey? _key;
    private ApiResponse? _response;
    private PossessionProof? _proof;
    private string? _identity;
    private string? _claimedClientId;
    private string? _baselineClientId;
    private int _acceptedBaseline;
    private int _rejectedBaseline;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    /// <summary>情境中要提出的新憑證（只提交公開部分；持有證明以其私鑰產生）。</summary>
    private X509Certificate2 Candidate => _candidate ??= SpikeCertificates.CreateSelfSignedClientCertificate($"proof-binding-{Runtime.EnvironmentName}-{Guid.NewGuid():N}");

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

    /// <summary>呼叫身分：「無憑證」、「未登錄憑證」，或某 Client 目前登錄的 mTLS 憑證。</summary>
    private static X509Certificate2? Identity(string identity) => identity switch
    {
        "無憑證" => null,
        "未登錄憑證" => Runtime.UnregisteredSelfSignedCertificate,
        _ => Runtime.ClientCertificate(identity),
    };

    private static X509Certificate2 ApplicantOf(string identity) => Identity(identity) ?? throw new InvalidOperationException("持有證明情境需要一個已登錄的 mTLS 身分。");

    [Given("持有證明綁定情境中記錄 {string} 的已受理申請數")]
    public async Task GivenRecordsAcceptedBaseline(string clientId)
    {
        _baselineClientId = clientId;
        _acceptedBaseline = await ApplicantVerificationSteps.AcceptedSubmissionCountAsync(clientId);
        _rejectedBaseline = await RejectedProofCountAsync(clientId);
    }

    [When("以 {string} 身分為 {string} 送出時間偏移 {int} 秒的持有證明新憑證申請")]
    public async Task WhenSubmitsWithTimeOffset(string identity, string claimedClientId, int offsetSeconds)
    {
        var timestamp = ApplicantProofs.NowUnix() + offsetSeconds;
        var proof = ApplicantProofs.CertificateProofAt(claimedClientId, ApplicantOf(identity).Thumbprint, Candidate, Candidate, timestamp, ApplicantProofs.NewNonce());
        _response = await SubmitCertificateAsync(identity, claimedClientId, proof);
    }

    [When("以 {string} 身分為 {string} 送出持有證明新憑證申請並保留該證明")]
    public async Task WhenSubmitsAndKeepsProof(string identity, string claimedClientId)
    {
        _proof = FreshCertificateProof(identity, claimedClientId);
        _response = await SubmitCertificateAsync(identity, claimedClientId, _proof);
    }

    [When("以相同的持有證明重送新憑證申請")]
    public async Task WhenReplaysCertificateProof()
    {
        _proof.Should().NotBeNull("情境應先保留持有證明");
        _response = await SubmitCertificateAsync(_identity!, _claimedClientId!, _proof);
    }

    [When("以 {string} 身分為 {string} 送出綁定 {string} 申請者指紋的持有證明新憑證申請")]
    public async Task WhenSubmitsProofBoundToOtherApplicant(string identity, string claimedClientId, string boundClientId)
    {
        var proof = ApplicantProofs.CertificateProofAt(claimedClientId, Runtime.ClientCertificate(boundClientId).Thumbprint, Candidate, Candidate, ApplicantProofs.NowUnix(), ApplicantProofs.NewNonce());
        _response = await SubmitCertificateAsync(identity, claimedClientId, proof);
    }

    [When("以 {string} 身分為 {string} 送出缺漏時間戳與 nonce 的持有證明新憑證申請")]
    public async Task WhenSubmitsWithoutTiming(string identity, string claimedClientId)
    {
        var proof = FreshCertificateProof(identity, claimedClientId).WithoutTiming();
        _response = await SubmitCertificateAsync(identity, claimedClientId, proof);
    }

    [When("以 {string} 身分為 {string} 送出持有證明簽章金鑰申請並保留該證明")]
    public async Task WhenSubmitsSigningKeyAndKeepsProof(string identity, string claimedClientId)
    {
        _key = Runtime.CreateSigningKey(claimedClientId);
        _proof = ApplicantProofs.SigningKeyProofAt(claimedClientId, ApplicantOf(identity).Thumbprint, _key.KeyId, _key.Key, _key.Key, ApplicantProofs.NowUnix(), ApplicantProofs.NewNonce());
        _identity = identity;
        _claimedClientId = claimedClientId;
        _response = await SubmitSigningKeyAsync(identity, claimedClientId, _key, _proof);
    }

    [When("以相同的持有證明重送簽章金鑰申請")]
    public async Task WhenReplaysSigningKeyProof()
    {
        _proof.Should().NotBeNull("情境應先保留持有證明");
        _key.Should().NotBeNull("情境應先建立簽章金鑰");
        _response = await SubmitSigningKeyAsync(_identity!, _claimedClientId!, _key!, _proof);
    }

    [Then("持有證明申請回應為 {int}")]
    public void ThenProofResponseIs(int expected)
    {
        _response.Should().NotBeNull("情境應先送出申請");
        ((int)_response!.Status).Should().Be(expected, _response.Body);
    }

    [Then("{string} 的已受理申請數相對基準增加 {int}")]
    public async Task ThenAcceptedCountIncreasedBy(string clientId, int expectedIncrease)
    {
        _baselineClientId.Should().Be(clientId, "情境應先記錄該 Client 的受理數基準");
        (await ApplicantVerificationSteps.AcceptedSubmissionCountAsync(clientId) - _acceptedBaseline).Should().Be(expectedIncrease, "被拒絕的持有證明不應新增待核准紀錄");
    }

    [Then("{string} 的持有證明拒絕稽核數增加 {int}")]
    public async Task ThenRejectedProofAuditIncreasedBy(string clientId, int expectedIncrease)
    {
        _baselineClientId.Should().Be(clientId, "情境應先記錄該 Client 的稽核基準");
        (await RejectedProofCountAsync(clientId) - _rejectedBaseline).Should().Be(expectedIncrease, "被拒絕的持有證明嘗試仍應寫入稽核");
    }

    [Then("持有證明規格文件包含 {string}")]
    public void ThenSpecContains(string expected)
    {
        File.ReadAllText(Path.Combine(RepoRoot, SpecRelativePath)).Should().Contain(expected);
    }

    [Then("持有證明 ADR 包含 {string}")]
    public void ThenAdrContains(string expected)
    {
        File.ReadAllText(Path.Combine(RepoRoot, AdrRelativePath)).Should().Contain(expected);
    }

    private PossessionProof FreshCertificateProof(string identity, string claimedClientId)
        => ApplicantProofs.CertificateProofAt(claimedClientId, ApplicantOf(identity).Thumbprint, Candidate, Candidate, ApplicantProofs.NowUnix(), ApplicantProofs.NewNonce());

    private Task<ApiResponse> SubmitCertificateAsync(string identity, string claimedClientId, PossessionProof? proof)
    {
        _identity = identity;
        _claimedClientId = claimedClientId;
        _proof = proof;
        return ApplicantProofs.SubmitCertificateAsync(Runtime, Identity(identity), claimedClientId, Candidate.ExportCertificatePem(), proof);
    }

    private static Task<ApiResponse> SubmitSigningKeyAsync(string identity, string claimedClientId, SignatureKey key, PossessionProof? proof)
        => ApplicantProofs.SubmitSigningKeyAsync(Runtime, Identity(identity), claimedClientId, key.KeyId, key.Key.ExportSubjectPublicKeyInfoPem(), proof);

    /// <summary>計算指定 Client 被拒絕、且原因為持有證明無效的登錄申請稽核數（憑證與簽章金鑰合計）。</summary>
    private static async Task<int> RejectedProofCountAsync(string clientId)
    {
        var response = await AdministratorInterfaceSteps.SendAdminAsync(Runtime.AdministratorCertificate, HttpMethod.Get, AuditRecordsPath);
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        using var document = JsonDocument.Parse(response.Body);
        return document.RootElement.EnumerateArray()
            .Count(record => record.GetProperty("operation").GetString() is "certificate.submit" or "signing_key.submit"
                && record.GetProperty("outcome").GetString() == "rejected"
                && record.GetProperty("reason").GetString() == ProofRejectionReason
                && record.GetProperty("clientId").GetString() == clientId);
    }
}
