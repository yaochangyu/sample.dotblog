using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AuthSpike.Certificates;
using AuthSpike.Hosting;
using AuthSpike.Tests.Support;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

/// <summary>12 單：憑證登錄申請與管理員核准／拒絕。所有呼叫皆經真實 mTLS 與 HTTP 路徑，不直接呼叫登錄內部方法。</summary>
[Binding]
public sealed class CertificateRegistrationApprovalSteps
{
    private const string ClientId = "orders-client";
    private const string SubmitRelativePath = "client-certificate-requests";
    private const string AdminRequestsRelativePath = "admin/client-certificate-requests";
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/12-certificate-registration-approval.md";

    private X509Certificate2? _candidate;
    private string? _requestId;
    private ApiResponse? _submission;
    private ApiResponse? _adminResponse;
    private ApiResponse? _business;
    private bool _tokenIssued;
    private string? _token;
    private string? _baseline;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    /// <summary>待核准的候選憑證（只在本情境中產生；申請時只提交其公開部分）。</summary>
    private X509Certificate2 Candidate => _candidate ??= SpikeCertificates.CreateSelfSignedClientCertificate($"{ClientId}-candidate-{Runtime.EnvironmentName}-{Guid.NewGuid():N}");

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

    /// <summary>情境中使用的呼叫身分。「待核准憑證」即申請者本人的候選憑證。</summary>
    private X509Certificate2? Identity(string identity) => identity switch
    {
        "無憑證" => null,
        "未登錄憑證" => Runtime.UnregisteredSelfSignedCertificate,
        "Client 憑證" => Runtime.ClientCertificate(ClientId),
        "管理員憑證" => Runtime.AdministratorCertificate,
        "待核准憑證" => Candidate,
        _ => throw new ArgumentOutOfRangeException(nameof(identity), identity, "未知的呼叫身分"),
    };

    [Given("為 {string} 提交只含公開憑證的登錄申請")]
    public async Task GivenSubmitsPublicCertificate(string clientId)
    {
        await SubmitPublicCertificateAsync(clientId);
        _submission!.Status.Should().Be(HttpStatusCode.Accepted, _submission.Body);
    }

    [When("為 {string} 提交只含公開憑證的登錄申請")]
    public Task WhenSubmitsPublicCertificate(string clientId) => SubmitPublicCertificateAsync(clientId);

    [When("為 {string} 提交含私鑰的登錄申請")]
    public async Task WhenSubmitsCertificateWithPrivateKey(string clientId)
    {
        using var key = Candidate.GetRSAPrivateKey() ?? throw new InvalidOperationException("候選憑證必須含私鑰以模擬違規申請。");
        _submission = await SubmitAsync(clientId, Candidate.ExportCertificatePem() + key.ExportPkcs8PrivateKeyPem());
    }

    [Then("登錄申請回應為 {int}")]
    public void ThenSubmissionResponseIs(int expected)
    {
        _submission.Should().NotBeNull("情境應先提交登錄申請");
        ((int)_submission!.Status).Should().Be(expected, _submission.Body);
    }

    [When("以 {string} 身分核准該申請")]
    public async Task WhenIdentityApproves(string identity)
    {
        _adminResponse = await AdministratorInterfaceSteps.SendAdminAsync(Identity(identity), HttpMethod.Post, $"{AdminRequestsRelativePath}/{_requestId}/approve");
    }

    [When("以 {string} 身分拒絕該申請")]
    public async Task WhenIdentityRejects(string identity)
    {
        _adminResponse = await AdministratorInterfaceSteps.SendAdminAsync(Identity(identity), HttpMethod.Post, $"{AdminRequestsRelativePath}/{_requestId}/reject");
    }

    [Then("管理員操作回應為 {int}")]
    public void ThenAdministratorOperationResponseIs(int expected)
    {
        _adminResponse.Should().NotBeNull("情境應先呼叫管理員操作");
        ((int)_adminResponse!.Status).Should().Be(expected, _adminResponse.Body);
    }

    [Then("管理員查詢申請狀態為 {string}")]
    public async Task ThenAdministratorSeesStatus(string status)
    {
        var response = await AdministratorInterfaceSteps.SendAdminAsync(Runtime.AdministratorCertificate, HttpMethod.Get, $"{AdminRequestsRelativePath}/{_requestId}");
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        using var document = JsonDocument.Parse(response.Body);
        document.RootElement.GetProperty("status").GetString().Should().Be(status switch
        {
            "待核准" => "pending",
            "已核准" => "approved",
            "已拒絕" => "rejected",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "未知的申請狀態"),
        });
    }

    [Given("記錄信任名單基準")]
    public async Task GivenRecordsTrustListBaseline()
    {
        _baseline = await AdministratorInterfaceSteps.TrustListBodyAsync();
    }

    [Then("信任名單與核准前基準相同")]
    public async Task ThenTrustListUnchangedFromBaseline()
    {
        _baseline.Should().NotBeNull("情境應先記錄信任名單基準");
        (await AdministratorInterfaceSteps.TrustListBodyAsync()).Should().Be(_baseline);
    }

    [Then("信任名單中 {string} 包含待核准憑證指紋")]
    public async Task ThenTrustListIncludesCandidate(string clientId)
    {
        using var document = JsonDocument.Parse(await AdministratorInterfaceSteps.TrustListBodyAsync());
        document.RootElement.GetProperty("clients").EnumerateArray()
            .Single(client => client.GetProperty("clientId").GetString() == clientId)
            .GetProperty("certificateThumbprints").EnumerateArray()
            .Select(thumbprint => thumbprint.GetString())
            .Should().Contain(Candidate.Thumbprint);
    }

    [Then("信任名單不包含待核准憑證指紋")]
    public async Task ThenTrustListExcludesCandidate()
    {
        AdministratorInterfaceSteps.RegisteredThumbprints(await AdministratorInterfaceSteps.TrustListBodyAsync())
            .Should().NotContain(Candidate.Thumbprint);
    }

    [Then("信任名單中除 {string} 外與核准前基準相同")]
    public async Task ThenOtherClientsUnchangedFromBaseline(string clientId)
    {
        _baseline.Should().NotBeNull("情境應先記錄信任名單基準");
        OtherClientEntries(await AdministratorInterfaceSteps.TrustListBodyAsync(), clientId)
            .Should().Equal(OtherClientEntries(_baseline!, clientId));
    }

    [When("以 {string} 的 mTLS 憑證要求 {string} 的 Token")]
    public async Task WhenRequestsTokenWithCertificate(string identity, string clientId)
    {
        (_tokenIssued, _token) = await AdministratorInterfaceSteps.RequestTokenAsync(Identity(identity), clientId);
    }

    [Then("Token 要求未核發")]
    public void ThenTokenNotIssued()
    {
        _tokenIssued.Should().BeFalse();
    }

    [Then("Token 要求已核發")]
    public void ThenTokenIssued()
    {
        _tokenIssued.Should().BeTrue("已核准的憑證應能取得 Token");
        _token.Should().NotBeNullOrEmpty();
    }

    [Then("{string} 的 Client 憑證仍可取得 Token")]
    public async Task ThenClientCertificateStillIssuesToken(string clientId)
    {
        var (issued, _) = await AdministratorInterfaceSteps.RequestTokenAsync(Runtime.ClientCertificate(clientId), clientId);
        issued.Should().BeTrue($"{clientId} 的既有憑證不應因其他 Client 的核准而改變");
    }

    [When("以 {string} 的 mTLS 憑證持該 Token 送出建立訂單請求")]
    public async Task WhenSendsOrderWithCertificateAndToken(string identity)
    {
        _token.Should().NotBeNullOrEmpty("情境應先取得 Token");
        var json = $$"""{"orderReference":"{{Guid.NewGuid():N}}","item":"book","quantity":1}""";
        using var request = await SignedHttp.BuildSignedAsync(
            HttpMethod.Post,
            new Uri($"https://localhost:{Runtime.OrdersApi.Port}/orders"),
            _token,
            Runtime.SigningKey(ClientId),
            json,
            Guid.NewGuid().ToString());
        using var client = SignedHttp.CreateClient(Runtime, Identity(identity));
        _business = await SignedHttp.SendAsync(client, request);
    }

    [Then("建立訂單回應為 {int}")]
    public void ThenOrderResponseIs(int expected)
    {
        _business.Should().NotBeNull("情境應先送出建立訂單請求");
        ((int)_business!.Status).Should().Be(expected, _business.Body);
    }

    [Then("12 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    [Then("12 單第 {int} 項驗收已勾選")]
    public void ThenAcceptanceItemChecked(int index)
    {
        var boxes = Regex.Matches(IssueText, @"^- \[( |x)\] ", RegexOptions.Multiline);
        boxes.Count.Should().BeGreaterThanOrEqualTo(index);
        boxes[index - 1].Groups[1].Value.Should().Be("x");
    }

    private async Task SubmitPublicCertificateAsync(string clientId)
    {
        _submission = await SubmitAsync(clientId, Candidate.ExportCertificatePem());
        if (_submission.Status == HttpStatusCode.Accepted)
        {
            using var document = JsonDocument.Parse(_submission.Body);
            _requestId = document.RootElement.GetProperty("requestId").GetString();
        }
    }

    private static async Task<ApiResponse> SubmitAsync(string clientId, string publicCertificatePem)
    {
        var body = JsonSerializer.Serialize(new { clientId, publicCertificatePem });
        using var client = SignedHttp.CreateClient(Runtime, null);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Runtime.AuthServer.Issuer, SubmitRelativePath))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        return await SignedHttp.SendAsync(client, request);
    }

    private static List<string> OtherClientEntries(string body, string clientId)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("clients").EnumerateArray()
            .Where(client => client.GetProperty("clientId").GetString() != clientId)
            .Select(client => client.GetRawText())
            .ToList();
    }
}
