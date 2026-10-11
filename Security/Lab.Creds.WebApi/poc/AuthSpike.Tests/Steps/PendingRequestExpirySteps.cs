using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using AuthSpike.Certificates;
using AuthSpike.Hosting;
using AuthSpike.Tests.Support;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>
/// 待核准申請的逾期處理（19 單的延伸，lab 暫定 24 小時，待使用者確認）。逾期以可注入的時鐘判定，情境不真的等待。
/// 所有呼叫皆經真實 mTLS 與 HTTP 管理端點，不直接呼叫內部登錄方法。
/// </summary>
[Binding]
public sealed class PendingRequestExpirySteps
{
    private const string CertificateKind = "憑證";
    private const string SigningKeyKind = "簽章金鑰";

    /// <summary>情境中已受理的申請（類型與申請識別），依提出順序排列。</summary>
    private readonly List<(string Kind, string RequestId)> _accepted = new();
    private ApiResponse? _response;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    private static ManualTimeProvider Clock => Runtime.Clock as ManualTimeProvider
        ?? throw new InvalidOperationException("逾期情境須標示 @isolated，才會使用可推進的時鐘。");

    [Given("逾期情境中為 {string} 以 {string} 提出 {int} 筆待核准申請")]
    public async Task GivenSubmitsPendingRequests(string clientId, string kind, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await SubmitAsync(kind, clientId, clientId);
            _response!.Status.Should().Be(HttpStatusCode.Accepted, _response.Body);
        }
    }

    [When("逾期情境時鐘推進 {int} 分鐘")]
    public void WhenClockAdvances(int minutes) => Clock.Advance(TimeSpan.FromMinutes(minutes));

    [When("逾期情境中以 {string} 身分為 {string} 再提出一筆新 {string} 登錄申請")]
    public Task WhenSubmitsAgain(string identity, string claimedClientId, string kind)
        => SubmitAsync(kind, identity, claimedClientId);

    [When("逾期情境中管理員嘗試核准第 {int} 筆申請")]
    public Task WhenAdministratorTriesToApprove(int index) => TryDecideAsync(index, "approve");

    [When("逾期情境中管理員嘗試{string}第 {int} 筆申請")]
    public Task WhenAdministratorTriesDecision(string action, int index)
        => TryDecideAsync(index, action == "核准" ? "approve" : "reject");

    [Then("逾期情境回應為 {int}")]
    public void ThenResponseIs(int expected)
    {
        _response.Should().NotBeNull("情境應先送出申請或管理操作");
        ((int)_response!.Status).Should().Be(expected, _response.Body);
    }

    [Then("逾期情境錯誤為 {string}")]
    public void ThenErrorIs(string expected)
    {
        _response.Should().NotBeNull("情境應先送出申請或管理操作");
        using var document = JsonDocument.Parse(_response!.Body);
        document.RootElement.GetProperty("error").GetString().Should().Be(expected);
    }

    [Then("逾期情境中管理員查詢第 {int} 筆申請狀態為 {string}")]
    public async Task ThenAdministratorSeesStatus(int index, string expectedStatus)
    {
        _accepted.Count.Should().BeGreaterThanOrEqualTo(index, "情境應先受理足夠的申請");
        var (kind, requestId) = _accepted[index - 1];
        var response = await AdministratorInterfaceSteps.SendAdminAsync(Runtime.AdministratorCertificate, HttpMethod.Get, $"{RequestsPath(kind)}/{requestId}");
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        using var document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("status").GetString().Should().Be(expectedStatus);
    }

    private async Task SubmitAsync(string kind, string identity, string claimedClientId)
    {
        if (kind == CertificateKind)
        {
            var certificate = SpikeCertificates.CreateSelfSignedClientCertificate($"expiry-cert-{Runtime.EnvironmentName}-{Guid.NewGuid():N}");
            var proof = ApplicantProofs.CertificateProof(claimedClientId, certificate);
            _response = await ApplicantProofs.SubmitCertificateAsync(Runtime, Runtime.ClientCertificate(identity), claimedClientId, certificate.ExportCertificatePem(), proof);
        }
        else
        {
            kind.Should().Be(SigningKeyKind, "申請類型只能是憑證或簽章金鑰");
            var keyId = $"expiry-{Guid.NewGuid():N}";
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var proof = ApplicantProofs.SigningKeyProof(claimedClientId, keyId, key);
            _response = await ApplicantProofs.SubmitSigningKeyAsync(Runtime, Runtime.ClientCertificate(identity), claimedClientId, keyId, key.ExportSubjectPublicKeyInfoPem(), proof);
        }

        if (_response.Status == HttpStatusCode.Accepted)
        {
            using var document = JsonDocument.Parse(_response.Body);
            _accepted.Add((kind, document.RootElement.GetProperty("requestId").GetString()!));
        }
    }

    private async Task TryDecideAsync(int index, string decision)
    {
        _accepted.Count.Should().BeGreaterThanOrEqualTo(index, "情境應先受理足夠的申請");
        var (kind, requestId) = _accepted[index - 1];
        _response = await AdministratorInterfaceSteps.SendAdminAsync(Runtime.AdministratorCertificate, HttpMethod.Post, $"{RequestsPath(kind)}/{requestId}/{decision}");
    }

    private static string RequestsPath(string kind) => kind == CertificateKind
        ? "admin/client-certificate-requests"
        : "admin/signing-key-requests";
}
