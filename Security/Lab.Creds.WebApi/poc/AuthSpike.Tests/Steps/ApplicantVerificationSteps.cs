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

/// <summary>17 單：憑證登錄申請的申請者驗證與持有證明。所有呼叫皆經真實 mTLS 與 HTTP 管理端點，不直接呼叫內部登錄方法。</summary>
[Binding]
public sealed class ApplicantVerificationSteps
{
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/17-certificate-request-applicant-verification.md";
    private const string AuditRecordsPath = "admin/audit-records";
    private const string CertificateRequestsPath = "admin/client-certificate-requests";

    private X509Certificate2? _candidate;
    private ApiResponse? _response;
    private ApiResponse? _firstUnauthenticatedResponse;
    private string? _requestId;
    private string? _existingRequestId;
    private int _acceptedBaseline;
    private string? _baselineClientId;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    /// <summary>情境中要提出的新憑證（只提交公開部分；持有證明以其私鑰產生）。</summary>
    private X509Certificate2 Candidate => _candidate ??= SpikeCertificates.CreateSelfSignedClientCertificate($"applicant-candidate-{Runtime.EnvironmentName}-{Guid.NewGuid():N}");

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
    /// 呼叫身分：「無憑證」、「未登錄憑證」，或某 Client 目前登錄的 mTLS 憑證（取最新登錄者；被撤銷或退役後仍是同一張憑證）。
    /// </summary>
    private static X509Certificate2? Identity(string identity) => identity switch
    {
        "無憑證" => null,
        "未登錄憑證" => Runtime.UnregisteredSelfSignedCertificate,
        _ => Runtime.ClientCertificate(identity),
    };

    [When("以 {string} 身分為 {string} 提出持有證明為 {string} 的新憑證登錄申請")]
    public async Task WhenSubmitsNewCertificate(string identity, string claimedClientId, string proofMode)
    {
        _response = await SubmitAsync(identity, claimedClientId, proofMode);
        CaptureRequestId(_response);
    }

    [When("無憑證分別為 {string} 與 {string} 提出持有證明為 {string} 的新憑證登錄申請")]
    public async Task WhenUnauthenticatedSubmitsForTwoClients(string unknownClientId, string existingClientId, string proofMode)
    {
        _firstUnauthenticatedResponse = await SubmitAsync("無憑證", unknownClientId, proofMode);
        _response = await SubmitAsync("無憑證", existingClientId, proofMode);
    }

    [Given("為 {string} 已有待核准的新憑證登錄申請")]
    public async Task GivenClientHasPendingRequest(string clientId)
    {
        var response = await SubmitAsync(clientId, clientId, "有效");
        response.Status.Should().Be(HttpStatusCode.Accepted, response.Body);
        _existingRequestId = RequestIdOf(response.Body);
    }

    [Given("記錄 {string} 已受理的登錄申請稽核數")]
    public async Task GivenRecordsAcceptedSubmissions(string clientId)
    {
        _baselineClientId = clientId;
        _acceptedBaseline = await AcceptedSubmissionCountAsync(clientId);
    }

    [Then("{string} 已受理的登錄申請稽核數不變")]
    public async Task ThenAcceptedSubmissionsUnchanged(string clientId)
    {
        _baselineClientId.Should().Be(clientId, "情境應先記錄該 Client 的稽核數基準");
        (await AcceptedSubmissionCountAsync(clientId)).Should().Be(_acceptedBaseline, "被拒絕的申請不應建立待核准紀錄");
    }

    [Then("申請者驗證回應為 {int}")]
    public void ThenApplicantResponseIs(int expected)
    {
        _response.Should().NotBeNull("情境應先提出登錄申請");
        ((int)_response!.Status).Should().Be(expected, _response.Body);
    }

    [Then("失敗回應不含既有待核准申請的識別")]
    public void ThenFailureDoesNotLeakExistingRequest()
    {
        _response.Should().NotBeNull("情境應先提出登錄申請");
        _existingRequestId.Should().NotBeNullOrEmpty("情境應先建立既有的待核准申請");
        _response!.Body.Should().NotContain(_existingRequestId!);
    }

    [Then("管理員查詢待核准申請的已驗證申請者為 {string}")]
    public async Task ThenAdministratorSeesVerifiedApplicant(string clientId)
    {
        _requestId.Should().NotBeNullOrEmpty("情境應先建立待核准申請");
        var response = await AdministratorInterfaceSteps.SendAdminAsync(Runtime.AdministratorCertificate, HttpMethod.Get, $"{CertificateRequestsPath}/{_requestId}");
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        root.GetProperty("status").GetString().Should().Be("pending");
        root.GetProperty("applicantClientId").GetString().Should().Be(clientId);
        root.GetProperty("applicantThumbprint").GetString().Should().Be(Identity(clientId)!.Thumbprint);
    }

    [Then("未驗證的兩次申請回應狀態與內容相同")]
    public void ThenUnauthenticatedResponsesMatch()
    {
        _firstUnauthenticatedResponse.Should().NotBeNull("情境應先送出兩次未驗證申請");
        _response.Should().NotBeNull("情境應先送出兩次未驗證申請");
        _firstUnauthenticatedResponse!.Status.Should().Be(_response!.Status);
        _firstUnauthenticatedResponse.Body.Should().Be(_response.Body);
        ((int)_response.Status).Should().Be(401);
    }

    [Given("管理員撤銷 {string} 的原始憑證")]
    public async Task GivenAdministratorRevokesOriginalCertificate(string clientId)
    {
        var thumbprint = Runtime.ClientCertificate(clientId).Thumbprint;
        var response = await AdministratorInterfaceSteps.SendAdminAsync(Runtime.AdministratorCertificate, HttpMethod.Post, $"admin/clients/{clientId}/certificates/{thumbprint}/revoke");
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
    }

    [Given("管理員退役 {string} 的原始憑證")]
    public async Task GivenAdministratorRetiresOriginalCertificate(string clientId)
    {
        var thumbprint = Runtime.ClientCertificate(clientId).Thumbprint;
        var response = await AdministratorInterfaceSteps.SendAdminAsync(Runtime.AdministratorCertificate, HttpMethod.Post, $"admin/clients/{clientId}/certificates/{thumbprint}/retire");
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
    }

    [Given("管理員停用 {string}")]
    public async Task GivenAdministratorDisablesClient(string clientId)
    {
        var response = await AdministratorInterfaceSteps.SendAdminAsync(Runtime.AdministratorCertificate, HttpMethod.Post, $"admin/clients/{clientId}/disable");
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
    }

    [Then("17 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    [Then("17 單第 {int} 項驗收已勾選")]
    public void ThenAcceptanceItemChecked(int index)
    {
        var boxes = Regex.Matches(IssueText, @"^- \[( |x)\] ", RegexOptions.Multiline);
        boxes.Count.Should().BeGreaterThanOrEqualTo(index);
        boxes[index - 1].Groups[1].Value.Should().Be("x");
    }

    /// <summary>
    /// 送出新憑證登錄申請。持有證明模式：「有效」以新憑證私鑰簽章；「缺漏」不附證明；「他人私鑰」以無關憑證的私鑰簽章。
    /// </summary>
    private Task<ApiResponse> SubmitAsync(string identity, string claimedClientId, string proofMode)
    {
        var proof = proofMode switch
        {
            "有效" => ApplicantProofs.CertificateProof(claimedClientId, Candidate),
            "缺漏" => null,
            "他人私鑰" => ApplicantProofs.ForeignCertificateProof(claimedClientId, Candidate, Runtime.UnregisteredSelfSignedCertificate),
            _ => throw new ArgumentOutOfRangeException(nameof(proofMode), proofMode, "未知的持有證明模式"),
        };
        return ApplicantProofs.SubmitCertificateAsync(Runtime, Identity(identity), claimedClientId, Candidate.ExportCertificatePem(), proof);
    }

    private void CaptureRequestId(ApiResponse response)
    {
        if (response.Status == HttpStatusCode.Accepted)
        {
            _requestId = RequestIdOf(response.Body);
        }
    }

    private static string RequestIdOf(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("requestId").GetString()!;
    }

    /// <summary>計算指定 Client 已受理（accepted）的憑證或簽章金鑰登錄申請稽核數；受理前會寫入稽核，因此數量等於已建立的待核准紀錄數。</summary>
    private static async Task<int> AcceptedSubmissionCountAsync(string clientId)
    {
        var response = await AdministratorInterfaceSteps.SendAdminAsync(Runtime.AdministratorCertificate, HttpMethod.Get, AuditRecordsPath);
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        using var document = JsonDocument.Parse(response.Body);
        return document.RootElement.EnumerateArray()
            .Count(record => record.GetProperty("operation").GetString() is "certificate.submit" or "signing_key.submit"
                && record.GetProperty("outcome").GetString() == "accepted"
                && record.GetProperty("clientId").GetString() == clientId);
    }
}
