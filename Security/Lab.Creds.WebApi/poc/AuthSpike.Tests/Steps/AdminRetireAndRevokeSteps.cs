using System.Diagnostics;
using System.Net;
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
/// 14 單：管理員退役與撤銷憑證與簽章金鑰。管理操作與替代項目的申請核准皆經真實 mTLS 與 HTTP 路徑，
/// 不直接呼叫授權伺服器的內部登錄或信任登錄物件。
/// </summary>
[Binding]
public sealed class AdminRetireAndRevokeSteps : IDisposable
{
    private const string ClientId = "orders-client";
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/14-admin-retire-and-revoke.md";
    private const string UnknownThumbprint = "0000000000000000000000000000000000000000";
    private const string UnknownKeyId = "no-such-signing-key";
    private static readonly TimeSpan MaxBlockDelay = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    private readonly Dictionary<string, HttpClient> _connections = new(StringComparer.Ordinal);
    private X509Certificate2? _replacementCertificate;
    private SignatureKey? _replacementKey;
    private string? _token;
    private string? _baseline;
    private ApiResponse? _adminResponse;
    private ApiResponse? _orderResponse;
    private Stopwatch? _revocationClock;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    private static X509Certificate2 OriginalCertificate => Runtime.ClientCertificate(ClientId);

    private static SignatureKey OriginalKey => Runtime.SigningKey(ClientId);

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
    private X509Certificate2? Identity(string identity) => identity switch
    {
        "無憑證" => null,
        "未登錄憑證" => Runtime.UnregisteredSelfSignedCertificate,
        "Client 憑證" => OriginalCertificate,
        "管理員憑證" => Runtime.AdministratorCertificate,
        "原始憑證" => OriginalCertificate,
        "替代憑證" => _replacementCertificate ?? throw new InvalidOperationException("情境應先建立替代憑證"),
        _ => throw new ArgumentOutOfRangeException(nameof(identity), identity, "未知的呼叫身分"),
    };

    private SignatureKey KeyOf(string label) => label switch
    {
        "原始簽章金鑰" => OriginalKey,
        "替代簽章金鑰" => _replacementKey ?? throw new InvalidOperationException("情境應先建立替代簽章金鑰"),
        _ => throw new ArgumentOutOfRangeException(nameof(label), label, "未知的簽章金鑰"),
    };

    [Given("{string} 已有經核准的替代憑證並進入重疊期")]
    public async Task GivenApprovedReplacementCertificate(string clientId)
    {
        _replacementCertificate = SpikeCertificates.CreateSelfSignedClientCertificate($"{clientId}-replacement-{Runtime.EnvironmentName}-{Guid.NewGuid():N}");
        var body = JsonSerializer.Serialize(new { clientId, publicCertificatePem = _replacementCertificate.ExportCertificatePem() });
        var submitted = await PostJsonAsync("client-certificate-requests", body, null);
        submitted.Status.Should().Be(HttpStatusCode.Accepted, submitted.Body);

        var approved = await AdministratorInterfaceSteps.SendAdminAsync(
            Runtime.AdministratorCertificate,
            HttpMethod.Post,
            $"admin/client-certificate-requests/{RequestIdOf(submitted.Body)}/approve");
        approved.Status.Should().Be(HttpStatusCode.OK, approved.Body);
    }

    [Given("{string} 已有經核准的替代簽章金鑰並進入重疊期")]
    public async Task GivenApprovedReplacementSigningKey(string clientId)
    {
        _replacementKey = Runtime.CreateSigningKey(clientId);
        var body = JsonSerializer.Serialize(new
        {
            clientId,
            keyId = _replacementKey.KeyId,
            publicKeyPem = _replacementKey.Key.ExportSubjectPublicKeyInfoPem(),
        });
        var submitted = await PostJsonAsync("signing-key-requests", body, null);
        submitted.Status.Should().Be(HttpStatusCode.Accepted, submitted.Body);

        var approved = await AdministratorInterfaceSteps.SendAdminAsync(
            Runtime.AdministratorCertificate,
            HttpMethod.Post,
            $"admin/signing-key-requests/{RequestIdOf(submitted.Body)}/approve");
        approved.Status.Should().Be(HttpStatusCode.OK, approved.Body);
    }

    [Given("{string} 以原始憑證取得 Token 並保持既有連線")]
    public async Task GivenTokenOnExistingConnection(string clientId)
    {
        var response = await RequestTokenAsync(OriginalCertificate, clientId);
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
        _token = AccessTokenOf(response.Body);
    }

    [Given("記錄退役撤銷信任名單基準")]
    public async Task GivenRecordsBaseline()
    {
        _baseline = await AdministratorInterfaceSteps.TrustListBodyAsync();
    }

    [When("以 {string} 身分退役 {string} 的原始憑證")]
    public Task WhenIdentityRetiresCertificate(string identity, string clientId)
        => AdminCertificateAsync(Identity(identity), "retire", clientId, Runtime.ClientCertificate(clientId).Thumbprint);

    [When("以 {string} 身分撤銷 {string} 的原始憑證")]
    public Task WhenIdentityRevokesCertificate(string identity, string clientId)
        => AdminCertificateAsync(Identity(identity), "revoke", clientId, Runtime.ClientCertificate(clientId).Thumbprint);

    [When("管理員退役 {string} 的原始憑證")]
    public Task WhenAdministratorRetiresCertificate(string clientId)
        => AdminCertificateAsync(Runtime.AdministratorCertificate, "retire", clientId, Runtime.ClientCertificate(clientId).Thumbprint);

    [When("管理員撤銷 {string} 的原始憑證")]
    public Task WhenAdministratorRevokesCertificate(string clientId)
        => AdminCertificateAsync(Runtime.AdministratorCertificate, "revoke", clientId, Runtime.ClientCertificate(clientId).Thumbprint);

    [When("管理員撤銷 {string} 的不存在憑證")]
    public Task WhenAdministratorRevokesUnknownCertificate(string clientId)
        => AdminCertificateAsync(Runtime.AdministratorCertificate, "revoke", clientId, UnknownThumbprint);

    [When("管理員撤銷不存在的 Client {string} 的憑證")]
    public Task WhenAdministratorRevokesUnknownClientCertificate(string clientId)
        => AdminCertificateAsync(Runtime.AdministratorCertificate, "revoke", clientId, UnknownThumbprint);

    [When("以 {string} 身分退役 {string} 的原始簽章金鑰")]
    public Task WhenIdentityRetiresSigningKey(string identity, string clientId)
        => AdminSigningKeyAsync(Identity(identity), "retire", clientId, Runtime.SigningKey(clientId).KeyId);

    [When("以 {string} 身分撤銷 {string} 的原始簽章金鑰")]
    public Task WhenIdentityRevokesSigningKey(string identity, string clientId)
        => AdminSigningKeyAsync(Identity(identity), "revoke", clientId, Runtime.SigningKey(clientId).KeyId);

    [When("管理員退役 {string} 的原始簽章金鑰")]
    public Task WhenAdministratorRetiresSigningKey(string clientId)
        => AdminSigningKeyAsync(Runtime.AdministratorCertificate, "retire", clientId, Runtime.SigningKey(clientId).KeyId);

    [When("管理員撤銷 {string} 的原始簽章金鑰")]
    public Task WhenAdministratorRevokesSigningKey(string clientId)
        => AdminSigningKeyAsync(Runtime.AdministratorCertificate, "revoke", clientId, Runtime.SigningKey(clientId).KeyId);

    [When("管理員撤銷 {string} 的不存在簽章金鑰")]
    public Task WhenAdministratorRevokesUnknownSigningKey(string clientId)
        => AdminSigningKeyAsync(Runtime.AdministratorCertificate, "revoke", clientId, UnknownKeyId);

    [Then("退役撤銷管理操作回應為 {int}")]
    public void ThenRetireRevokeResponseIs(int expected)
    {
        _adminResponse.Should().NotBeNull("情境應先執行管理操作");
        ((int)_adminResponse!.Status).Should().Be(expected, _adminResponse.Body);
    }

    [Then("{string} 要求 {string} 的 Token 被拒絕")]
    public async Task ThenTokenRequestRejected(string identity, string clientId)
    {
        var response = await RequestTokenAsync(Identity(identity), clientId);
        ((int)response.Status).Should().BeInRange(400, 499, response.Body);
        response.Body.Should().NotContain("access_token");
    }

    [Then("{string} 要求 {string} 的 Token 成功")]
    public async Task ThenTokenRequestSucceeds(string identity, string clientId)
    {
        var response = await RequestTokenAsync(Identity(identity), clientId);
        response.Status.Should().Be(HttpStatusCode.OK, response.Body);
    }

    [Given("以 {string} 送出建立訂單請求回應為 {int}")]
    [Then("以 {string} 送出建立訂單請求回應為 {int}")]
    public async Task ThenOrderWithKeyResponseIs(string keyLabel, int expected)
    {
        _token.Should().NotBeNullOrEmpty("情境應先以原始憑證取得 Token");
        var response = await CreateOrderAsync(OriginalCertificate, _token!, KeyOf(keyLabel));
        ((int)response.Status).Should().Be(expected, response.Body);
    }

    [Then("既有連線的建立訂單請求在 60 秒內被阻擋")]
    public async Task ThenExistingConnectionBlockedWithinSixtySeconds()
    {
        _revocationClock.Should().NotBeNull("情境應先執行撤銷");
        _token.Should().NotBeNullOrEmpty("情境應先以原始憑證取得 Token");

        // 撤銷後持續以同一條既有連線送出請求，直到被阻擋或超過 60 秒上限，以實測撤銷延遲。
        ApiResponse? response = null;
        while (_revocationClock!.Elapsed < MaxBlockDelay)
        {
            response = await CreateOrderAsync(OriginalCertificate, _token!, OriginalKey);
            if (response.Status == HttpStatusCode.Unauthorized)
            {
                break;
            }

            await Task.Delay(PollInterval);
        }

        response.Should().NotBeNull();
        response!.Status.Should().Be(HttpStatusCode.Unauthorized, response.Body);
        _revocationClock.Elapsed.Should().BeLessThan(MaxBlockDelay, "撤銷後須於 60 秒內阻擋既有連線");
    }

    [Then("信任名單與退役撤銷基準相同")]
    public async Task ThenTrustListUnchangedFromBaseline()
    {
        _baseline.Should().NotBeNull("情境應先記錄退役撤銷信任名單基準");
        (await AdministratorInterfaceSteps.TrustListBodyAsync()).Should().Be(_baseline);
    }

    [Then("信任名單中除 {string} 外與退役撤銷基準相同")]
    public async Task ThenOtherClientsUnchangedFromBaseline(string clientId)
    {
        _baseline.Should().NotBeNull("情境應先記錄退役撤銷信任名單基準");
        OtherClientEntries(await AdministratorInterfaceSteps.TrustListBodyAsync(), clientId)
            .Should().Equal(OtherClientEntries(_baseline!, clientId));
    }

    [Then("{string} 的 Client 以自身憑證與簽章金鑰建立訂單回應為 {int}")]
    public async Task ThenClientOrdersWithOwnCredentials(string clientId, int expected)
    {
        var certificate = Runtime.ClientCertificate(clientId);
        var token = await RequestTokenAsync(certificate, clientId);
        token.Status.Should().Be(HttpStatusCode.OK, token.Body);

        var response = await CreateOrderAsync(certificate, AccessTokenOf(token.Body), Runtime.SigningKey(clientId));
        ((int)response.Status).Should().Be(expected, response.Body);
    }

    [Then("14 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    [Then("14 單第 {int} 項驗收已勾選")]
    public void ThenAcceptanceItemChecked(int index)
    {
        var boxes = Regex.Matches(IssueText, @"^- \[( |x)\] ", RegexOptions.Multiline);
        boxes.Count.Should().BeGreaterThanOrEqualTo(index);
        boxes[index - 1].Groups[1].Value.Should().Be("x");
    }

    public void Dispose()
    {
        foreach (var connection in _connections.Values)
        {
            connection.Dispose();
        }

        _connections.Clear();
    }

    private async Task AdminCertificateAsync(X509Certificate2? certificate, string action, string clientId, string thumbprint)
    {
        _revocationClock = action == "revoke" ? Stopwatch.StartNew() : _revocationClock;
        _adminResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            certificate,
            HttpMethod.Post,
            $"admin/clients/{clientId}/certificates/{thumbprint}/{action}");
    }

    private async Task AdminSigningKeyAsync(X509Certificate2? certificate, string action, string clientId, string keyId)
    {
        _revocationClock = action == "revoke" ? Stopwatch.StartNew() : _revocationClock;
        _adminResponse = await AdministratorInterfaceSteps.SendAdminAsync(
            certificate,
            HttpMethod.Post,
            $"admin/clients/{clientId}/signing-keys/{keyId}/{action}");
    }

    private async Task<ApiResponse> RequestTokenAsync(X509Certificate2? certificate, string clientId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Runtime.AuthServer.Issuer, "connect/token"))
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
            }),
        };
        return await SendAsync(certificate, request);
    }

    private async Task<ApiResponse> CreateOrderAsync(X509Certificate2 certificate, string token, SignatureKey key)
    {
        var json = $$"""{"orderReference":"{{Guid.NewGuid():N}}","item":"book","quantity":1}""";
        using var request = await SignedHttp.BuildSignedAsync(
            HttpMethod.Post,
            new Uri($"https://localhost:{Runtime.OrdersApi.Port}/orders"),
            token,
            key,
            json,
            Guid.NewGuid().ToString());
        return await SendAsync(certificate, request);
    }

    private async Task<ApiResponse> PostJsonAsync(string relativePath, string body, X509Certificate2? certificate)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Runtime.AuthServer.Issuer, relativePath))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        return await SendAsync(certificate, request);
    }

    /// <summary>同一張憑證共用一條既有連線（keep-alive），讓撤銷後的既有連線行為可被實測。</summary>
    private async Task<ApiResponse> SendAsync(X509Certificate2? certificate, HttpRequestMessage request)
    {
        var client = certificate is null ? SignedHttp.CreateClient(Runtime, null) : ConnectionFor(certificate);
        return await SignedHttp.SendAsync(client, request);
    }

    private HttpClient ConnectionFor(X509Certificate2 certificate)
    {
        if (!_connections.TryGetValue(certificate.Thumbprint, out var client))
        {
            client = SignedHttp.CreateClient(Runtime, certificate);
            _connections[certificate.Thumbprint] = client;
        }

        return client;
    }

    private static string RequestIdOf(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("requestId").GetString()!;
    }

    private static string AccessTokenOf(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("access_token").GetString()!;
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
