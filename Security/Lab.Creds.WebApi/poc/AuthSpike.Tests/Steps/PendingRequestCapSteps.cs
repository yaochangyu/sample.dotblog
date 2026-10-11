using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthSpike.Certificates;
using AuthSpike.Hosting;
using AuthSpike.Tests.Support;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>19 單：每個 Client 的待核准申請數上限。所有呼叫皆經真實 mTLS 與 HTTP 管理端點，不直接呼叫內部登錄方法。</summary>
[Binding]
public sealed class PendingRequestCapSteps
{
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/19-pending-request-cap.md";
    private const string CertificateKind = "certificate";
    private const string SigningKeyKind = "signing-key";

    /// <summary>情境中已受理的申請（類型與申請識別），依提出順序排列。</summary>
    private readonly List<(string Kind, string RequestId)> _accepted = new();
    private ApiResponse? _response;

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

    [Given("為 {string} 已提出 {int} 筆待核准的憑證登錄申請")]
    public async Task GivenSubmitsPendingCertificateRequests(string clientId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await SubmitCertificateAsync(clientId, clientId);
            _response!.Status.Should().Be(HttpStatusCode.Accepted, _response.Body);
        }
    }

    [Given("為 {string} 已提出 {int} 筆待核准的簽章金鑰登錄申請")]
    public async Task GivenSubmitsPendingSigningKeyRequests(string clientId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await SubmitSigningKeyAsync(clientId, clientId);
            _response!.Status.Should().Be(HttpStatusCode.Accepted, _response.Body);
        }
    }

    [When("以 {string} 身分為 {string} 再提出一筆新憑證登錄申請")]
    public Task WhenSubmitsCertificateRequest(string identity, string claimedClientId)
        => SubmitCertificateAsync(identity, claimedClientId);

    [When("以 {string} 身分為 {string} 再提出一筆新簽章金鑰登錄申請")]
    public Task WhenSubmitsSigningKeyRequest(string identity, string claimedClientId)
        => SubmitSigningKeyAsync(identity, claimedClientId);

    [When("管理員核准第 {int} 筆待核准申請")]
    public Task WhenAdministratorApprovesRequest(int index) => DecideAsync(index, "approve");

    [When("管理員拒絕第 {int} 筆待核准申請")]
    public Task WhenAdministratorRejectsRequest(int index) => DecideAsync(index, "reject");

    [Then("待核准上限測試回應為 {int}")]
    public void ThenResponseIs(int expected)
    {
        _response.Should().NotBeNull("情境應先提出申請");
        ((int)_response!.Status).Should().Be(expected, _response.Body);
    }

    [Then("待核准上限測試錯誤為 {string}")]
    public void ThenErrorIs(string expected)
    {
        _response.Should().NotBeNull("情境應先提出申請");
        using var document = JsonDocument.Parse(_response!.Body);
        document.RootElement.GetProperty("error").GetString().Should().Be(expected);
    }

    [Then("管理員查詢第 {int} 筆待核准申請狀態為 {string}")]
    public async Task ThenAdministratorSeesStatus(int index, string expectedStatus)
    {
        _accepted.Count.Should().BeGreaterThanOrEqualTo(index, "情境應先受理足夠的申請");
        var (kind, requestId) = _accepted[index - 1];
        var response = await AdministratorInterfaceSteps.SendAdminAsync(Runtime.AdministratorCertificate, HttpMethod.Get, $"{RequestsPath(kind)}/{requestId}");
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        using var document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("status").GetString().Should().Be(expectedStatus);
    }

    [Then("19 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    [Then("19 單第 {int} 項驗收已勾選")]
    public void ThenAcceptanceItemChecked(int index)
    {
        var boxes = Regex.Matches(IssueText, @"^- \[( |x)\] ", RegexOptions.Multiline);
        boxes.Count.Should().BeGreaterThanOrEqualTo(index);
        boxes[index - 1].Groups[1].Value.Should().Be("x");
    }

    private async Task SubmitCertificateAsync(string identity, string claimedClientId)
    {
        var certificate = SpikeCertificates.CreateSelfSignedClientCertificate($"pending-cap-cert-{Runtime.EnvironmentName}-{Guid.NewGuid():N}");
        var proof = ApplicantProofs.CertificateProof(claimedClientId, certificate);
        _response = await ApplicantProofs.SubmitCertificateAsync(Runtime, Runtime.ClientCertificate(identity), claimedClientId, certificate.ExportCertificatePem(), proof);
        TrackIfAccepted(CertificateKind, _response);
    }

    private async Task SubmitSigningKeyAsync(string identity, string claimedClientId)
    {
        var keyId = $"pending-cap-{Guid.NewGuid():N}";
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var proof = ApplicantProofs.SigningKeyProof(claimedClientId, keyId, key);
        _response = await ApplicantProofs.SubmitSigningKeyAsync(Runtime, Runtime.ClientCertificate(identity), claimedClientId, keyId, key.ExportSubjectPublicKeyInfoPem(), proof);
        TrackIfAccepted(SigningKeyKind, _response);
    }

    private async Task DecideAsync(int index, string decision)
    {
        _accepted.Count.Should().BeGreaterThanOrEqualTo(index, "情境應先受理足夠的申請");
        var (kind, requestId) = _accepted[index - 1];
        var response = await AdministratorInterfaceSteps.SendAdminAsync(Runtime.AdministratorCertificate, HttpMethod.Post, $"{RequestsPath(kind)}/{requestId}/{decision}");
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
    }

    private void TrackIfAccepted(string kind, ApiResponse response)
    {
        if (response.Status == HttpStatusCode.Accepted)
        {
            using var document = JsonDocument.Parse(response.Body);
            _accepted.Add((kind, document.RootElement.GetProperty("requestId").GetString()!));
        }
    }

    private static string RequestsPath(string kind) => kind == CertificateKind
        ? "admin/client-certificate-requests"
        : "admin/signing-key-requests";
}
