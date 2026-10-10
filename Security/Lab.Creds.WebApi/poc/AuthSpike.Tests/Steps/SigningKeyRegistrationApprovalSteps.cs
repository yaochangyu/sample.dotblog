using System.Net;
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

/// <summary>13 單：簽章金鑰登錄申請與管理員核准／拒絕。所有呼叫皆經真實 mTLS 與 HTTP 路徑，不直接呼叫登錄內部方法。</summary>
[Binding]
public sealed class SigningKeyRegistrationApprovalSteps
{
    private const string ClientId = "orders-client";
    private const string SubmitRelativePath = "signing-key-requests";
    private const string AdminRequestsRelativePath = "admin/signing-key-requests";
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/13-signing-key-registration-approval.md";

    private SignatureKey? _pendingKey;
    private string? _requestId;
    private string? _token;
    private ApiResponse? _submission;
    private ApiResponse? _adminResponse;
    private ApiResponse? _business;

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

    /// <summary>情境中使用的呼叫身分（與 12 單相同的集合，「待核准憑證」由 12 單 Steps 管理，本檔不使用）。</summary>
    private static X509Certificate2? Identity(string identity) => identity switch
    {
        "無憑證" => null,
        "未登錄憑證" => Runtime.UnregisteredSelfSignedCertificate,
        "Client 憑證" => Runtime.ClientCertificate(ClientId),
        "管理員憑證" => Runtime.AdministratorCertificate,
        _ => throw new ArgumentOutOfRangeException(nameof(identity), identity, "未知的呼叫身分"),
    };

    [Given("簽章金鑰情境中 {string} 以 mTLS 取得 Token")]
    public async Task GivenSigningKeyScenarioCallerHasToken(string clientId)
    {
        var (issued, token) = await AdministratorInterfaceSteps.RequestTokenAsync(Runtime.ClientCertificate(clientId), clientId);
        issued.Should().BeTrue($"{clientId} 的既有憑證應能取得 Token");
        _token = token;
    }

    [Given("為 {string} 提交只含公開金鑰的簽章金鑰申請")]
    public async Task GivenSubmitsPublicSigningKey(string clientId)
    {
        await SubmitPublicSigningKeyAsync(clientId);
        _submission!.Status.Should().Be(HttpStatusCode.Accepted, _submission.Body);
    }

    [When("為 {string} 提交只含公開金鑰的簽章金鑰申請")]
    public Task WhenSubmitsPublicSigningKey(string clientId) => SubmitPublicSigningKeyAsync(clientId);

    [When("為 {string} 提交含私鑰的簽章金鑰申請")]
    public async Task WhenSubmitsSigningKeyWithPrivateKey(string clientId)
    {
        _pendingKey = Runtime.CreateSigningKey(clientId);
        var privatePem = _pendingKey.Key.ExportPkcs8PrivateKeyPem();
        _submission = await SubmitAsync(clientId, _pendingKey.KeyId, privatePem);
    }

    [Then("簽章金鑰申請回應為 {int}")]
    public void ThenSigningKeySubmissionResponseIs(int expected)
    {
        _submission.Should().NotBeNull("情境應先提交簽章金鑰申請");
        ((int)_submission!.Status).Should().Be(expected, _submission.Body);
    }

    [When("以 {string} 身分核准簽章金鑰申請")]
    public async Task WhenIdentityApprovesSigningKey(string identity)
    {
        _adminResponse = await AdministratorInterfaceSteps.SendAdminAsync(Identity(identity), HttpMethod.Post, $"{AdminRequestsRelativePath}/{_requestId}/approve");
    }

    [When("以 {string} 身分拒絕簽章金鑰申請")]
    public async Task WhenIdentityRejectsSigningKey(string identity)
    {
        _adminResponse = await AdministratorInterfaceSteps.SendAdminAsync(Identity(identity), HttpMethod.Post, $"{AdminRequestsRelativePath}/{_requestId}/reject");
    }

    [Then("簽章金鑰管理員操作回應為 {int}")]
    public void ThenSigningKeyAdministratorOperationResponseIs(int expected)
    {
        _adminResponse.Should().NotBeNull("情境應先呼叫管理員操作");
        ((int)_adminResponse!.Status).Should().Be(expected, _adminResponse.Body);
    }

    [Then("管理員查詢簽章金鑰申請狀態為 {string}")]
    public async Task ThenAdministratorSeesSigningKeyStatus(string status)
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

    [When("以申請中的簽章金鑰送出建立訂單請求")]
    public async Task WhenSendsOrderWithPendingSigningKey()
    {
        _pendingKey.Should().NotBeNull("情境應先提交簽章金鑰申請");
        _token.Should().NotBeNullOrEmpty("情境應先取得 Token");
        _business = await SendOrderAsync(ClientId, _token!, _pendingKey!);
    }

    [When("以 {string} 的 Token 送出以申請中簽章金鑰簽署的建立訂單請求")]
    public async Task WhenSendsOrderWithOtherClientTokenAndPendingSigningKey(string clientId)
    {
        _pendingKey.Should().NotBeNull("情境應先提交簽章金鑰申請");
        var (issued, token) = await AdministratorInterfaceSteps.RequestTokenAsync(Runtime.ClientCertificate(clientId), clientId);
        issued.Should().BeTrue($"{clientId} 的既有憑證應能取得 Token");
        _business = await SendOrderAsync(clientId, token!, _pendingKey!);
    }

    [Then("簽章金鑰建立訂單回應為 {int}")]
    public void ThenSigningKeyOrderResponseIs(int expected)
    {
        _business.Should().NotBeNull("情境應先送出建立訂單請求");
        ((int)_business!.Status).Should().Be(expected, _business.Body);
    }

    [Given("為 {string} 提交與 {string} 既有簽章金鑰 keyId 相同的簽章金鑰申請")]
    public Task GivenSubmitsSigningKeyWithOtherClientKeyId(string clientId, string keyOwnerClientId)
        => SubmitWithKeyIdAsync(clientId, Runtime.SigningKey(keyOwnerClientId).KeyId);

    [Given("為 {string} 提交與其既有簽章金鑰 keyId 相同的簽章金鑰申請")]
    public Task GivenSubmitsSigningKeyWithOwnKeyId(string clientId)
        => SubmitWithKeyIdAsync(clientId, Runtime.SigningKey(clientId).KeyId);

    [When("以已登錄的簽章金鑰送出建立訂單請求")]
    public async Task WhenSendsOrderWithRegisteredSigningKey()
    {
        _token.Should().NotBeNullOrEmpty("情境應先取得 Token");
        _business = await SendOrderAsync(ClientId, _token!, Runtime.SigningKey(ClientId));
    }

    [Then("13 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    [Then("13 單第 {int} 項驗收已勾選")]
    public void ThenAcceptanceItemChecked(int index)
    {
        var boxes = Regex.Matches(IssueText, @"^- \[( |x)\] ", RegexOptions.Multiline);
        boxes.Count.Should().BeGreaterThanOrEqualTo(index);
        boxes[index - 1].Groups[1].Value.Should().Be("x");
    }

    /// <summary>以指定 keyId 提交新的公開金鑰（用於模擬與既有金鑰 keyId 重複的申請）。</summary>
    private async Task SubmitWithKeyIdAsync(string clientId, string keyId)
    {
        _pendingKey = Runtime.CreateSigningKey(clientId);
        _submission = await SubmitAsync(clientId, keyId, _pendingKey.Key.ExportSubjectPublicKeyInfoPem());
        _submission.Status.Should().Be(HttpStatusCode.Accepted, _submission.Body);
        using var document = JsonDocument.Parse(_submission.Body);
        _requestId = document.RootElement.GetProperty("requestId").GetString();
    }

    private async Task SubmitPublicSigningKeyAsync(string clientId)
    {
        _pendingKey = Runtime.CreateSigningKey(clientId);
        _submission = await SubmitAsync(clientId, _pendingKey.KeyId, _pendingKey.Key.ExportSubjectPublicKeyInfoPem());
        if (_submission.Status == HttpStatusCode.Accepted)
        {
            using var document = JsonDocument.Parse(_submission.Body);
            _requestId = document.RootElement.GetProperty("requestId").GetString();
        }
    }

    private static async Task<ApiResponse> SubmitAsync(string clientId, string keyId, string publicKeyPem)
    {
        var body = JsonSerializer.Serialize(new { clientId, keyId, publicKeyPem });
        using var client = SignedHttp.CreateClient(Runtime, null);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Runtime.AuthServer.Issuer, SubmitRelativePath))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        return await SignedHttp.SendAsync(client, request);
    }

    /// <summary>以指定 Client 的 mTLS 憑證與 Token 送出已簽章的建立訂單請求。</summary>
    private static async Task<ApiResponse> SendOrderAsync(string clientId, string token, SignatureKey key)
    {
        var json = $$"""{"orderReference":"{{Guid.NewGuid():N}}","item":"book","quantity":1}""";
        using var request = await SignedHttp.BuildSignedAsync(
            HttpMethod.Post,
            new Uri($"https://localhost:{Runtime.OrdersApi.Port}/orders"),
            token,
            key,
            json,
            Guid.NewGuid().ToString());
        using var client = SignedHttp.CreateClient(Runtime, Runtime.ClientCertificate(clientId));
        return await SignedHttp.SendAsync(client, request);
    }
}
