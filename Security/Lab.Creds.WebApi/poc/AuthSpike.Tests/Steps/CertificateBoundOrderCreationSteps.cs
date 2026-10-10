using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using AuthSpike.Hosting;
using AuthSpike.Signing;
using AuthSpike.Tests.Support;
using AwesomeAssertions;
using Reqnroll;

namespace AuthSpike.Tests.Steps;

[Binding]
public sealed class CertificateBoundOrderCreationSteps
{
    private const string OrderJson = """{"item":"book","quantity":1}""";
    private const string ForgedCertHeader = "X-Client-Cert-Sha256";
    private const string ClientIdHeader = "X-Client-Id";

    private string? _accessToken;
    private HttpStatusCode? _tokenStatus;
    private string _tokenBody = string.Empty;
    private HttpStatusCode? _orderStatus;
    private string _orderBody = string.Empty;
    private string? _orderLocation;
    private string _introspectionBody = string.Empty;
    private HttpStatusCode? _introspectionStatus;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    [Given("授權伺服器與建立訂單 API 已啟動")]
    public void GivenServersAreRunning()
    {
        Runtime.AuthServer.Port.Should().BeGreaterThan(0);
        Runtime.OrdersApi.Port.Should().BeGreaterThan(0);
    }

    [Given("已登錄服務 {string} 與其用戶端憑證")]
    public void GivenRegisteredClient(string clientId)
    {
        var certificate = Runtime.ClientCertificate(clientId);
        certificate.HasPrivateKey.Should().BeTrue();
    }

    [Given("呼叫端已以 {string} 憑證取得 Token")]
    public async Task GivenClientHasToken(string certificateName)
    {
        await RequestTokenAsync(certificateName);
        _accessToken.Should().NotBeNullOrEmpty();
    }

    [When("呼叫端以 {string} 向授權伺服器要求 Token")]
    public async Task WhenClientRequestsToken(string certificateName)
    {
        await RequestTokenAsync(certificateName);
    }

    [When("持有 Token 的呼叫端以 {string} 送出建立訂單請求")]
    public async Task WhenTokenHolderCreatesOrder(string certificateName)
    {
        await CreateOrderAsync(ResolveCertificate(certificateName), forgedHeaderValue: null);
    }

    [When("持有 Token 的呼叫端以 {string} 送出建立訂單請求，Body 與 X-Client-Id 標頭宣稱為 {string}")]
    public async Task WhenTokenHolderClaimsOtherClientIdentity(string certificateName, string claimedClientId)
    {
        var body = $$"""{"item":"book","quantity":1,"clientId":"{{claimedClientId}}"}""";
        await SendOrderAsync(ResolveCertificate(certificateName), _accessToken, body, [new(ClientIdHeader, claimedClientId)]);
    }

    [When("持有無效 Token 的呼叫端以 {string} 送出建立訂單請求")]
    public async Task WhenInvalidTokenHolderCreatesOrder(string certificateName)
    {
        await SendOrderAsync(ResolveCertificate(certificateName), "forged-token-not-issued", OrderJson, []);
    }

    [When("呼叫端以 API Key 送出建立訂單請求")]
    public async Task WhenApiKeyCallerCreatesOrder()
    {
        await SendOrderAsync(certificate: null, bearer: null, OrderJson, [new("X-Api-Key", "lab-api-key-without-trust")]);
    }

    [When("持有 Token 的呼叫端以 {string} 送出建立訂單請求，並附帶偽造的 X-Client-Cert-Sha256 標頭，標頭值為 {string} 憑證指紋")]
    public async Task WhenTokenHolderForgesCertificateHeader(string certificateName, string forgedCertificateName)
    {
        var forged = Base64UrlThumbprint(Runtime.ClientCertificate(forgedCertificateName));
        await CreateOrderAsync(ResolveCertificate(certificateName), forged);
    }

    [Then("授權伺服器不核發 Token")]
    public void ThenTokenIsNotIssued()
    {
        _tokenStatus.Should().NotBe(HttpStatusCode.OK);
        _accessToken.Should().BeNullOrEmpty();
    }

    [Then("授權伺服器核發 access_token")]
    public void ThenTokenIsIssued()
    {
        _tokenStatus.Should().Be(HttpStatusCode.OK, _tokenBody);
        _accessToken.Should().NotBeNullOrEmpty();
    }

    [Then("access_token 不是 JWT 格式")]
    public void ThenTokenIsOpaque()
    {
        _accessToken.Should().NotBeNullOrEmpty();
        _accessToken!.Split('.').Should().HaveCount(1);
    }

    [Then("內省結果的 cnf 憑證指紋與 {string} 憑證一致")]
    public async Task ThenIntrospectionCnfMatchesCertificate(string certificateName)
    {
        await IntrospectAsync(Runtime.ClientCertificate(SpikeRuntime.ResourceClientId));

        using var document = JsonDocument.Parse(_introspectionBody);
        document.RootElement.GetProperty("active").GetBoolean().Should().BeTrue();

        var cnf = document.RootElement.GetProperty("cnf");
        var cnfObject = cnf.ValueKind == JsonValueKind.String
            ? JsonDocument.Parse(cnf.GetString()!).RootElement
            : cnf;
        cnfObject.GetProperty("x5t#S256").GetString()
            .Should().Be(Base64UrlThumbprint(Runtime.ClientCertificate(certificateName)));
    }

    [Then("建立訂單 API 回應 201")]
    public void ThenOrderApiReturnsCreated()
    {
        _orderStatus.Should().Be(HttpStatusCode.Created);
    }

    [Then("建立訂單 API 回應 401")]
    public void ThenOrderApiReturnsUnauthorized()
    {
        _orderStatus.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Given(@"建立訂單契約 doc\/openapi.yml 定義 POST \/orders 回應 201 與 orderId")]
    public void GivenContractDefinesCreateOrder()
    {
        var contract = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "doc", "openapi.yml"));
        contract.Should().Contain("/orders:");
        contract.Should().Contain("operationId: createOrder");
        contract.Should().Contain("'201':");
        contract.Should().Contain("orderId:");
    }

    [Then("回應 Location 標頭以 {string} 開頭")]
    public void ThenLocationStartsWith(string prefix)
    {
        _orderLocation.Should().NotBeNull();
        _orderLocation!.Should().StartWith(prefix);
    }

    [Then("回應 orderId 為契約定義的 uuid 格式")]
    public void ThenOrderIdIsUuid()
    {
        using var document = JsonDocument.Parse(_orderBody);
        Guid.TryParse(document.RootElement.GetProperty("orderId").GetString(), out _).Should().BeTrue();
    }

    [Then("OpenIddict 伺服器元件版本為 {string}")]
    public void ThenOpenIddictVersionIs(string expectedVersion)
    {
        var version = typeof(OpenIddict.Server.OpenIddictServerOptions).Assembly.GetName().Version;
        version.Should().NotBeNull();
        $"{version!.Major}.{version.Minor}.{version.Build}".Should().Be(expectedVersion);
    }

    [Then("本 spike 以 .NET 10 目標框架建置")]
    public void ThenTargetsNet10()
    {
        var target = typeof(CertificateBoundOrderCreationSteps).Assembly
            .GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), false)
            .Cast<System.Runtime.Versioning.TargetFrameworkAttribute>()
            .Single()
            .FrameworkName;
        target.Should().Be(".NETCoreApp,Version=v10.0");
    }

    [Then("回應 clientId 為 {string}")]
    public void ThenResponseClientIdIs(string expectedClientId)
    {
        using var document = JsonDocument.Parse(_orderBody);
        document.RootElement.GetProperty("clientId").GetString().Should().Be(expectedClientId);
    }

    [Then("授權伺服器核發的 Token 效期為 {int} 秒")]
    public void ThenTokenLifetimeIs(int seconds)
    {
        using var document = JsonDocument.Parse(_tokenBody);
        // OpenIddict 回報的是剩餘秒數（例如 300 秒效期可能回報 299），容許向下取整造成的誤差。
        document.RootElement.GetProperty("expires_in").GetInt32().Should().BeInRange(seconds - 2, seconds);
    }

    [Then("各示範 Client 的用戶端憑證指紋彼此不同")]
    public void ThenDemoClientCertificatesAreDistinct()
    {
        var thumbprints = new[] { SpikeRuntime.OrdersClientId, SpikeRuntime.BillingClientId, SpikeRuntime.ResourceClientId }
            .Select(name => Base64UrlThumbprint(Runtime.ClientCertificate(name)))
            .ToList();
        thumbprints.Should().OnlyHaveUniqueItems();
    }

    [Then("各示範 Client 的用戶端憑證含私鑰")]
    public void ThenDemoClientCertificatesHavePrivateKeys()
    {
        foreach (var name in new[] { SpikeRuntime.OrdersClientId, SpikeRuntime.BillingClientId, SpikeRuntime.ResourceClientId })
        {
            Runtime.ClientCertificate(name).HasPrivateKey.Should().BeTrue(name);
        }
    }

    [When("呼叫端以 {string} 的 client_id 附帶 client_secret 且不附憑證向授權伺服器要求 Token")]
    public async Task WhenClientRequestsTokenWithClientSecret(string clientId)
    {
        using var client = CreateHttpClient(certificate: null);
        using var response = await client.PostAsync(
            new Uri(Runtime.AuthServer.Issuer, "connect/token"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = "lab-secret-that-must-not-work",
            }));

        _tokenStatus = response.StatusCode;
        _tokenBody = await response.Content.ReadAsStringAsync();
        _accessToken = null;
    }

    [When("等待 Token 到期")]
    public static Task WhenWaitForTokenExpiry() => Task.Delay(TimeSpan.FromSeconds(3));

    [When("未附用戶端憑證的呼叫端查詢 Token 內省")]
    public async Task WhenUncertifiedCallerIntrospects()
    {
        await IntrospectAsync(certificate: null);
    }

    [When("查詢 Token 內省")]
    public async Task WhenResourceIntrospects()
    {
        await IntrospectAsync(Runtime.ClientCertificate(SpikeRuntime.ResourceClientId));
    }

    [Then("內省端點拒絕該呼叫")]
    public void ThenIntrospectionRejectsCaller()
    {
        _introspectionStatus.Should().NotBe(HttpStatusCode.OK);
        _introspectionBody.Should().NotContain("\"active\":true");
    }

    [Then("內省結果為 active")]
    public void ThenIntrospectionIsActive()
    {
        _introspectionStatus.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(_introspectionBody);
        document.RootElement.GetProperty("active").GetBoolean().Should().BeTrue();
    }

    [Then("內省結果的到期時間晚於現在")]
    public void ThenIntrospectionExpiryIsInFuture()
    {
        using var document = JsonDocument.Parse(_introspectionBody);
        var exp = document.RootElement.GetProperty("exp").GetInt64();
        exp.Should().BeGreaterThan(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    [Then("內省結果的目標 API 為 {string}")]
    public void ThenIntrospectionAudienceIs(string expectedAudience)
    {
        using var document = JsonDocument.Parse(_introspectionBody);
        var aud = document.RootElement.GetProperty("aud");
        var audiences = aud.ValueKind == JsonValueKind.Array
            ? aud.EnumerateArray().Select(element => element.GetString()).ToList()
            : [aud.GetString()];
        audiences.Should().Contain(expectedAudience);
    }

    [Then("回應包含訂單編號")]
    public void ThenResponseContainsOrderId()
    {
        using var document = JsonDocument.Parse(_orderBody);
        Guid.TryParse(document.RootElement.GetProperty("orderId").GetString(), out var orderId).Should().BeTrue();
        orderId.Should().NotBe(Guid.Empty);
    }

    private static X509Certificate2? ResolveCertificate(string name) => name switch
    {
        "無憑證" => null,
        "其他 CA 簽發的憑證" => Runtime.OtherCaCertificate,
        "未登錄的自簽憑證" => Runtime.UnregisteredSelfSignedCertificate,
        _ => Runtime.ClientCertificate(name),
    };

    private async Task RequestTokenAsync(string certificateName)
    {
        var clientId = certificateName is "無憑證" or "其他 CA 簽發的憑證" or "未登錄的自簽憑證"
            ? SpikeRuntime.OrdersClientId
            : certificateName;

        using var client = CreateHttpClient(ResolveCertificate(certificateName));
        using var response = await client.PostAsync(
            new Uri(Runtime.AuthServer.Issuer, "connect/token"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
            }));

        _tokenStatus = response.StatusCode;
        _tokenBody = await response.Content.ReadAsStringAsync();
        _accessToken = null;
        if (response.IsSuccessStatusCode)
        {
            using var document = JsonDocument.Parse(_tokenBody);
            _accessToken = document.RootElement.GetProperty("access_token").GetString();
        }
    }

    private Task CreateOrderAsync(X509Certificate2? certificate, string? forgedHeaderValue)
    {
        var headers = forgedHeaderValue is null
            ? Array.Empty<KeyValuePair<string, string>>()
            : new[] { new KeyValuePair<string, string>(ForgedCertHeader, forgedHeaderValue) };
        return SendOrderAsync(certificate, _accessToken, OrderJson, headers);
    }

    private async Task SendOrderAsync(
        X509Certificate2? certificate,
        string? bearer,
        string body,
        IEnumerable<KeyValuePair<string, string>> headers)
    {
        using var client = CreateHttpClient(certificate);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"https://localhost:{Runtime.OrdersApi.Port}/orders"))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        if (bearer is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString());
        foreach (var header in headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }

        // 03 起所有業務呼叫都需請求簽章；Token 持有者以 orders-client 的簽章金鑰簽署，
        // 使 02 的負向案例仍只由 02 的因素（憑證綁定、目標 API、到期）決定拒絕。
        if (bearer is not null)
        {
            var now = DateTimeOffset.UtcNow;
            await BusinessRequestSigner.SignAsync(request, Runtime.SigningKey(SpikeRuntime.OrdersClientId), now, now.AddSeconds(60), Guid.NewGuid().ToString("N"));
        }

        using var response = await client.SendAsync(request);
        _orderStatus = response.StatusCode;
        _orderLocation = response.Headers.Location?.ToString();
        _orderBody = await response.Content.ReadAsStringAsync();
    }

    private async Task IntrospectAsync(X509Certificate2? certificate)
    {
        using var client = CreateHttpClient(certificate);
        using var response = await client.PostAsync(
            new Uri(Runtime.AuthServer.Issuer, "connect/introspect"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["token"] = _accessToken!,
                ["token_type_hint"] = "access_token",
                ["client_id"] = SpikeRuntime.ResourceClientId,
            }));

        _introspectionStatus = response.StatusCode;
        _introspectionBody = await response.Content.ReadAsStringAsync();
    }

    /// <summary>每次呼叫都使用新連線，確保 TLS 用戶端憑證依本次呼叫決定。</summary>
    private static HttpClient CreateHttpClient(X509Certificate2? certificate)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = Runtime.Trust.ServerCertificateValidator,
            ClientCertificateOptions = ClientCertificateOption.Manual,
        };
        if (certificate is not null)
        {
            handler.ClientCertificates.Add(certificate);
        }

        return new HttpClient(handler, disposeHandler: true);
    }

    private static string Base64UrlThumbprint(X509Certificate2 certificate)
        => Convert.ToBase64String(SHA256.HashData(certificate.RawData))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
