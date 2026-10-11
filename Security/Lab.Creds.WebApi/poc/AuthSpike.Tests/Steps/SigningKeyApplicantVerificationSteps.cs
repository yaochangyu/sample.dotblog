using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthSpike.Certificates;
using AuthSpike.Hosting;
using AuthSpike.Registration;
using AuthSpike.Signing;
using AuthSpike.Tests.Support;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>18 單：簽章金鑰登錄申請的申請者驗證與持有證明。所有呼叫皆經真實 mTLS 與 HTTP 管理端點，不直接呼叫內部登錄方法。</summary>
[Binding]
public sealed class SigningKeyApplicantVerificationSteps
{
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/18-signing-key-request-applicant-verification.md";
    private const string SigningKeyRequestsPath = "admin/signing-key-requests";

    private X509Certificate2? _pendingCertificate;
    private SignatureKey? _candidateKey;
    private ApiResponse? _response;
    private ApiResponse? _firstUnauthenticatedResponse;
    private string? _requestId;
    private string? _existingRequestId;
    private string? _existingKeyId;
    private int _acceptedBaseline;
    private string? _baselineClientId;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    /// <summary>情境中要提出的新簽章金鑰（只提交公開部分；持有證明以其私鑰產生）。</summary>
    private SignatureKey CandidateKey => _candidateKey ??= new SignatureKey($"candidate-{Guid.NewGuid():N}", ECDsa.Create(ECCurve.NamedCurves.nistP256));

    /// <summary>已提交待核准但尚未核准的憑證；它不在信任名單中，不能作為申請者身分。</summary>
    private X509Certificate2 PendingCertificate => _pendingCertificate ??= SpikeCertificates.CreateSelfSignedClientCertificate($"signing-key-applicant-pending-{Runtime.EnvironmentName}-{Guid.NewGuid():N}");

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

    /// <summary>
    /// 呼叫身分：「無憑證」、「未登錄憑證」、「待核准憑證」，或某 Client 目前登錄的 mTLS 憑證。
    /// </summary>
    private X509Certificate2? Identity(string identity) => identity switch
    {
        "無憑證" => null,
        "未登錄憑證" => Runtime.UnregisteredSelfSignedCertificate,
        "待核准憑證" => PendingCertificate,
        _ => Runtime.ClientCertificate(identity),
    };

    [Given("為 {string} 以待核准的新憑證提交登錄申請")]
    public async Task GivenSubmitsPendingCertificate(string clientId)
    {
        var submitted = await ApplicantProofs.SubmitCertificateAsync(
            Runtime,
            Runtime.ClientCertificate(clientId),
            clientId,
            PendingCertificate.ExportCertificatePem(),
            ApplicantProofs.CertificateProof(clientId, PendingCertificate));
        submitted.Status.Should().Be(HttpStatusCode.Accepted, submitted.Body);
    }

    [Given("為 {string} 已有待核准的新簽章金鑰登錄申請")]
    public async Task GivenClientHasPendingSigningKeyRequest(string clientId)
    {
        var response = await SubmitAsync(clientId, clientId, "有效");
        response.Status.Should().Be(HttpStatusCode.Accepted, response.Body);
        _existingRequestId = RequestIdOf(response.Body);
        _existingKeyId = CandidateKey.KeyId;
    }

    [Given("記錄 {string} 的已受理登錄稽核基準")]
    public async Task GivenRecordsAcceptedBaseline(string clientId)
    {
        _baselineClientId = clientId;
        _acceptedBaseline = await ApplicantVerificationSteps.AcceptedSubmissionCountAsync(clientId);
    }

    [Then("{string} 的已受理登錄稽核數仍為基準值")]
    public async Task ThenAcceptedCountUnchanged(string clientId)
    {
        _baselineClientId.Should().Be(clientId, "情境應先記錄該 Client 的稽核基準");
        (await ApplicantVerificationSteps.AcceptedSubmissionCountAsync(clientId)).Should().Be(_acceptedBaseline, "被拒絕的申請不應建立待核准紀錄");
    }

    [When("以 {string} 身分為 {string} 提出持有證明為 {string} 的新簽章金鑰登錄申請")]
    public async Task WhenSubmitsNewSigningKey(string identity, string claimedClientId, string proofMode)
    {
        _response = await SubmitAsync(identity, claimedClientId, proofMode);
        if (_response.Status == HttpStatusCode.Accepted)
        {
            _requestId = RequestIdOf(_response.Body);
        }
    }

    [When("無憑證分別為 {string} 與 {string} 提出持有證明為 {string} 的新簽章金鑰登錄申請")]
    public async Task WhenUnauthenticatedSubmitsForTwoClients(string unknownClientId, string existingClientId, string proofMode)
    {
        _firstUnauthenticatedResponse = await SubmitAsync("無憑證", unknownClientId, proofMode);
        _response = await SubmitAsync("無憑證", existingClientId, proofMode);
    }

    [Then("簽章金鑰申請者驗證回應為 {int}")]
    public void ThenApplicantResponseIs(int expected)
    {
        _response.Should().NotBeNull("情境應先提出簽章金鑰登錄申請");
        ((int)_response!.Status).Should().Be(expected, _response.Body);
    }

    [Then("失敗回應不含既有待核准簽章金鑰申請的識別")]
    public void ThenFailureDoesNotLeakExistingRequest()
    {
        _response.Should().NotBeNull("情境應先提出簽章金鑰登錄申請");
        _existingRequestId.Should().NotBeNullOrEmpty("情境應先建立既有的待核准簽章金鑰申請");
        _response!.Body.Should().NotContain(_existingRequestId!);
        _response.Body.Should().NotContain(_existingKeyId!);
    }

    [Then("管理員查詢簽章金鑰待核准申請的已驗證申請者為 {string}")]
    public async Task ThenAdministratorSeesVerifiedApplicant(string clientId)
    {
        _requestId.Should().NotBeNullOrEmpty("情境應先建立待核准的簽章金鑰申請");
        var response = await AdministratorInterfaceSteps.SendAdminAsync(Runtime.AdministratorCertificate, HttpMethod.Get, $"{SigningKeyRequestsPath}/{_requestId}");
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        root.GetProperty("status").GetString().Should().Be("pending");
        root.GetProperty("applicantClientId").GetString().Should().Be(clientId);
        root.GetProperty("applicantThumbprint").GetString().Should().Be(Identity(clientId)!.Thumbprint);
    }

    [Then("未驗證的簽章金鑰申請回應狀態與內容相同")]
    public void ThenUnauthenticatedResponsesMatch()
    {
        _firstUnauthenticatedResponse.Should().NotBeNull("情境應先送出兩次未驗證申請");
        _response.Should().NotBeNull("情境應先送出兩次未驗證申請");
        _firstUnauthenticatedResponse!.Status.Should().Be(_response!.Status);
        _firstUnauthenticatedResponse.Body.Should().Be(_response.Body);
        ((int)_response.Status).Should().Be(401);
    }

    [Then("18 單第 {int} 項驗收已勾選")]
    public void ThenAcceptanceItemChecked(int index)
    {
        var boxes = Regex.Matches(IssueText, @"^- \[( |x)\] ", RegexOptions.Multiline);
        boxes.Count.Should().BeGreaterThanOrEqualTo(index);
        boxes[index - 1].Groups[1].Value.Should().Be("x");
    }

    [Then("18 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    /// <summary>
    /// 送出簽章金鑰申請。持有證明模式：「有效」以申請中金鑰的私鑰對申請內容簽章；「缺漏」不附證明；「他人私鑰」以無關金鑰的私鑰簽章。
    /// </summary>
    private Task<ApiResponse> SubmitAsync(string identity, string claimedClientId, string proofMode)
    {
        var key = CandidateKey;
        // 持有證明綁定出示的申請者身分；無憑證時無申請者指紋，伺服器於證明檢查前即回 401。
        var applicantThumbprint = Identity(identity)?.Thumbprint ?? string.Empty;
        var proof = proofMode switch
        {
            "有效" => (PossessionProof?)ApplicantProofs.SigningKeyProofAt(claimedClientId, applicantThumbprint, key.KeyId, key.Key, key.Key, ApplicantProofs.NowUnix(), ApplicantProofs.NewNonce()),
            "缺漏" => null,
            "他人私鑰" => ForeignProof(claimedClientId, applicantThumbprint, key),
            _ => throw new ArgumentOutOfRangeException(nameof(proofMode), proofMode, "未知的持有證明模式"),
        };
        return ApplicantProofs.SubmitSigningKeyAsync(Runtime, Identity(identity), claimedClientId, key.KeyId, key.Key.ExportSubjectPublicKeyInfoPem(), proof);
    }

    /// <summary>以他人（非申請中金鑰）的私鑰對申請內容簽章，模擬無法證明持有該金鑰（18 單 AC-26）。</summary>
    private static PossessionProof ForeignProof(string clientId, string applicantThumbprint, SignatureKey key)
    {
        using var foreign = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return ApplicantProofs.SigningKeyProofAt(clientId, applicantThumbprint, key.KeyId, key.Key, foreign, ApplicantProofs.NowUnix(), ApplicantProofs.NewNonce());
    }

    private static string RequestIdOf(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("requestId").GetString()!;
    }
}
