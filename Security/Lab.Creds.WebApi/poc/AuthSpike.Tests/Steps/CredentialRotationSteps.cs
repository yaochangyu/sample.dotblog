using System.Net;
using System.Net.Http.Headers;
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

/// <summary>08 單：不中斷的正常輪替（mTLS 憑證與簽章金鑰分開、重疊期、退役）與洩漏撤銷的實際呼叫情境。</summary>
[Binding]
public sealed class CredentialRotationSteps : IDisposable
{
    private const string ClientId = "orders-client";
    private const string IssueRelativePath = ".scratch/server-to-server-api-protection/issues/08-credential-rotation.md";
    private const string LeakThresholdDescription = "60 秒";

    /// <summary>洩漏撤銷必須在此時間內被阻擋（08 單門檻，與 07 單一致）。</summary>
    private static readonly TimeSpan RevocationThreshold = TimeSpan.FromSeconds(60);

    /// <summary>一組呼叫端配置：執行環境、mTLS 憑證、請求簽章金鑰與目前 Token。</summary>
    private sealed record Caller(SpikeRuntime Runtime, X509Certificate2 Certificate, SignatureKey SigningKey, string? Token);

    private readonly Dictionary<string, Guid> _orders = new();
    private Caller? _original;
    private Caller? _current;
    private X509Certificate2? _pendingCertificate;
    private SignatureKey? _pendingKey;
    private SpikeRuntime? _secondary;
    private HttpStatusCode? _lastStatus;
    private string _lastBody = string.Empty;
    private bool _lastTokenIssued;
    private string? _retireError;
    private DateTimeOffset? _revokedAt;
    private string _primaryOrderName = string.Empty;

    private static SpikeRuntime Runtime => SpikeEnvironment.Runtime;

    [Given("08 單的實作紀錄可讀取")]
    public void GivenIssueRecordReadable()
    {
        File.Exists(Path.Combine(RepoRoot, IssueRelativePath)).Should().BeTrue();
    }

    [Given("另一個環境 {string} 的授權伺服器與建立訂單 API 已啟動")]
    public async Task GivenSecondEnvironmentStarted(string environmentName)
    {
        _secondary = await SpikeRuntime.StartAsync(
            SpikeEnvironment.FreeTcpPort(),
            SpikeEnvironment.FreeTcpPort(),
            environmentName: environmentName);
    }

    [Given("輪替情境中簽章呼叫端 {string} 以既有配置取得 Token 並成功建立訂單 {string}")]
    public async Task GivenCallerWithOriginalCredentialsCreatesOrder(string clientId, string orderName)
    {
        var certificate = Runtime.ClientCertificate(clientId);
        var signingKey = Runtime.SigningKey(clientId);
        var (issued, token) = await RequestTokenAsync(Runtime, certificate);
        issued.Should().BeTrue("既有配置應能取得 Token");

        _original = new Caller(Runtime, certificate, signingKey, token);
        _current = _original;
        _primaryOrderName = orderName;
        await CreateOrderAsync(_current, orderName);
    }

    [When("管理者為 {string} 產生並登錄新的 {string}")]
    public async Task WhenAdministratorGeneratesAndRegisters(string clientId, string kind)
    {
        var (rotatesCertificate, rotatesKey) = ParseKind(kind);
        if (rotatesCertificate)
        {
            _pendingCertificate = Runtime.CreateClientCertificate(clientId);
            await Runtime.RegisterClientCertificateAsync(clientId, _pendingCertificate);
        }

        if (rotatesKey)
        {
            _pendingKey = Runtime.CreateSigningKey(clientId);
            Runtime.RegisterSigningKey(clientId, _pendingKey);
        }
    }

    [When("管理者為 {string} 產生新的 {string} 但尚未登錄")]
    public void WhenAdministratorGeneratesWithoutRegistering(string clientId, string kind)
    {
        var (rotatesCertificate, rotatesKey) = ParseKind(kind);
        if (rotatesCertificate)
        {
            _pendingCertificate = Runtime.CreateClientCertificate(clientId);
        }

        if (rotatesKey)
        {
            _pendingKey = Runtime.CreateSigningKey(clientId);
        }
    }

    [When("管理者登錄新產生的 {string}")]
    public async Task WhenAdministratorRegistersPending(string kind)
    {
        var (rotatesCertificate, rotatesKey) = ParseKind(kind);
        if (rotatesCertificate)
        {
            await Runtime.RegisterClientCertificateAsync(ClientId, _pendingCertificate!);
        }

        if (rotatesKey)
        {
            Runtime.RegisterSigningKey(ClientId, _pendingKey!);
        }
    }

    [When("呼叫端切換為新的 {string}")]
    public void WhenCallerSwitchesTo(string kind)
    {
        var (rotatesCertificate, rotatesKey) = ParseKind(kind);
        var caller = _current!;
        if (rotatesCertificate)
        {
            caller = caller with { Certificate = _pendingCertificate! };
        }

        if (rotatesKey)
        {
            caller = caller with { SigningKey = _pendingKey! };
        }

        _current = caller;
    }

    [When("管理者嘗試在未登錄替代時退役 {string} 的既有 {string}")]
    public async Task WhenAdministratorTriesToRetireWithoutReplacement(string clientId, string kind)
    {
        var (rotatesCertificate, rotatesKey) = ParseKind(kind);
        try
        {
            if (rotatesCertificate)
            {
                await Runtime.RetireClientCertificateAsync(clientId, _original!.Certificate);
            }

            if (rotatesKey)
            {
                Runtime.RetireSigningKey(clientId, _original!.SigningKey);
            }

            _retireError = null;
        }
        catch (InvalidOperationException exception)
        {
            _retireError = exception.Message;
        }
    }

    [When("管理者退役 {string} 的既有 {string}")]
    public async Task WhenAdministratorRetires(string clientId, string kind)
    {
        var (rotatesCertificate, rotatesKey) = ParseKind(kind);
        if (rotatesCertificate)
        {
            await Runtime.RetireClientCertificateAsync(clientId, _original!.Certificate);
        }

        if (rotatesKey)
        {
            Runtime.RetireSigningKey(clientId, _original!.SigningKey);
        }
    }

    [When("呼叫端以尚未登錄的新 mTLS 憑證向授權伺服器要求 Token")]
    public async Task WhenCallerRequestsTokenWithUnregisteredCertificate()
    {
        (_lastTokenIssued, _) = await RequestTokenAsync(Runtime, _pendingCertificate!);
    }

    [When("呼叫端以尚未登錄的新簽章金鑰送出查詢訂單 {string}")]
    public async Task WhenCallerQueriesWithUnregisteredKey(string orderName)
    {
        (_lastStatus, _lastBody) = await QueryOrderAsync(_current!, _pendingKey!, orderName);
    }

    [When("呼叫端以目前 mTLS 憑證向授權伺服器要求 Token")]
    public async Task WhenCallerRequestsTokenWithCurrentCertificate()
    {
        var (issued, token) = await RequestTokenAsync(_current!.Runtime, _current.Certificate);
        _lastTokenIssued = issued;
        if (issued)
        {
            _current = _current with { Token = token };
        }
    }

    [When("呼叫端以既有配置向授權伺服器要求 Token")]
    public async Task WhenCallerRequestsTokenWithOriginalCertificate()
    {
        var (issued, token) = await RequestTokenAsync(_original!.Runtime, _original.Certificate);
        _lastTokenIssued = issued;
        if (issued)
        {
            _original = _original with { Token = token };
        }
    }

    [When("呼叫端以目前配置建立訂單 {string}")]
    public async Task WhenCallerCreatesOrderWithCurrentCredentials(string orderName)
    {
        await CreateOrderAsync(_current!, orderName);
    }

    [When("呼叫端以目前配置查詢訂單 {string}")]
    public async Task WhenCallerQueriesWithCurrentCredentials(string orderName)
    {
        (_lastStatus, _lastBody) = await QueryOrderAsync(_current!, _current!.SigningKey, orderName);
    }

    [When("呼叫端以既有配置查詢訂單 {string}")]
    public async Task WhenCallerQueriesWithOriginalCredentials(string orderName)
    {
        (_lastStatus, _lastBody) = await QueryOrderAsync(_original!, _original!.SigningKey, orderName);
    }

    [When("呼叫端以既有 Token 搭配目前配置查詢訂單 {string}")]
    public async Task WhenCallerQueriesWithOriginalTokenAndCurrentCredentials(string orderName)
    {
        var mixed = _current! with { Token = _original!.Token };
        (_lastStatus, _lastBody) = await QueryOrderAsync(mixed, mixed.SigningKey, orderName);
    }

    [When("呼叫端以 {string} 環境的 mTLS 憑證向該環境授權伺服器要求 Token")]
    public async Task WhenCallerRequestsTokenInEnvironment(string environmentName)
    {
        var runtime = RuntimeOf(environmentName);
        var certificate = runtime.ClientCertificate(ClientId);
        var (issued, token) = await RequestTokenAsync(runtime, certificate);
        _lastTokenIssued = issued;
        _current = new Caller(runtime, certificate, runtime.SigningKey(ClientId), token);
    }

    [When("呼叫端以 {string} 環境的簽章金鑰送出建立訂單請求")]
    public async Task WhenCallerCreatesOrderWithKeyFromEnvironment(string environmentName)
    {
        var key = RuntimeOf(environmentName).SigningKey(ClientId);
        var caller = _current!;
        (_lastStatus, _lastBody) = await CreateOrderRawAsync(caller with { SigningKey = key });
    }

    [When("疑似洩漏時撤銷 {string}")]
    public async Task WhenLeakIsRevoked(string target)
    {
        _revokedAt = DateTimeOffset.UtcNow;
        switch (target)
        {
            case "既有 mTLS 憑證":
                Runtime.Registry.RevokeCertificate(_original!.Certificate.Thumbprint);
                break;
            case "既有簽章金鑰":
                Runtime.Registry.RevokeSigningKey(_original!.SigningKey.KeyId);
                break;
            case "既有 Token":
                await Runtime.AuthServer.RevokeAccessTokenAsync(_original!.Token!);
                break;
            case "Client":
                Runtime.Registry.DisableClient(ClientId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(target), target, "未知的洩漏撤銷對象");
        }
    }

    [Then("08 單第 {int} 項驗收已勾選")]
    public void ThenIssueCheckboxIsChecked(int index)
    {
        CheckboxAt(index).Should().StartWith("- [x] ");
    }

    [Then("08 單的實作紀錄包含 {string}")]
    public void ThenIssueRecordContains(string expected)
    {
        IssueText.Should().Contain(expected);
    }

    [Then("08 單的實作紀錄不包含真實私鑰或憑證內容")]
    public void ThenIssueRecordHasNoKeyMaterial()
    {
        IssueText.Should().NotContain("-----BEGIN");
    }

    [Then("{string} 在 {string} 與 {string} 的 mTLS 憑證指紋互不相同")]
    public void ThenCertificateThumbprintsDiffer(string clientId, string first, string second)
    {
        RuntimeOf(first).ClientCertificate(clientId).Thumbprint
            .Should().NotBe(RuntimeOf(second).ClientCertificate(clientId).Thumbprint);
    }

    [Then("{string} 在 {string} 與 {string} 的簽章金鑰識別與公開金鑰互不相同")]
    public void ThenSigningKeysDifferAcrossEnvironments(string clientId, string first, string second)
    {
        var firstKey = RuntimeOf(first).SigningKey(clientId);
        var secondKey = RuntimeOf(second).SigningKey(clientId);
        firstKey.KeyId.Should().NotBe(secondKey.KeyId);
        firstKey.Key.ExportSubjectPublicKeyInfo().Should().NotEqual(secondKey.Key.ExportSubjectPublicKeyInfo());
    }

    [Then("{string} 與 {string} 的簽章金鑰識別互不相同")]
    public void ThenSigningKeyIdsDifferAcrossClients(string first, string second)
    {
        Runtime.SigningKey(first).KeyId.Should().NotBe(Runtime.SigningKey(second).KeyId);
    }

    [Then("退役被拒絕且原因為 {string}")]
    public void ThenRetirementRefused(string reason)
    {
        _retireError.Should().Be(reason);
    }

    [Then("輪替情境的建立訂單回應為 {int}")]
    public void ThenOrderResponseIs(int statusCode)
    {
        _lastStatus.Should().Be((HttpStatusCode)statusCode, _lastBody);
    }

    [Then("輪替情境的查詢回應為 {int}")]
    public void ThenQueryResponseIs(int statusCode)
    {
        _lastStatus.Should().Be((HttpStatusCode)statusCode, _lastBody);
    }

    [Then("輪替情境的 Token 要求已核發")]
    public void ThenTokenIssued()
    {
        _lastTokenIssued.Should().BeTrue();
    }

    [Then("輪替情境的 Token 要求未核發")]
    public void ThenTokenNotIssued()
    {
        _lastTokenIssued.Should().BeFalse();
    }

    [Then("輪替情境的 Token 要求被拒絕")]
    public void ThenTokenRequestRejected()
    {
        _lastTokenIssued.Should().BeFalse("退役後的舊配置不應再取得 Token");
    }

    [Then("既有 {string} 已列為退役")]
    public void ThenOriginalIsRetired(string kind)
    {
        var (rotatesCertificate, rotatesKey) = ParseKind(kind);
        if (rotatesCertificate)
        {
            Runtime.Registry.IsCertificateRetired(_original!.Certificate.Thumbprint).Should().BeTrue();
        }

        if (rotatesKey)
        {
            Runtime.Registry.IsSigningKeyRetired(_original!.SigningKey.KeyId).Should().BeTrue();
        }
    }

    [Then("既有 {string} 仍未列為退役")]
    public void ThenOriginalIsNotRetired(string kind)
    {
        var (rotatesCertificate, rotatesKey) = ParseKind(kind);
        if (rotatesCertificate)
        {
            Runtime.Registry.IsCertificateRetired(_original!.Certificate.Thumbprint).Should().BeFalse();
        }

        if (rotatesKey)
        {
            Runtime.Registry.IsSigningKeyRetired(_original!.SigningKey.KeyId).Should().BeFalse();
        }
    }

    [Then("既有配置尚未退役")]
    public void ThenOriginalCredentialsNotRetired()
    {
        Runtime.Registry.IsCertificateRetired(_original!.Certificate.Thumbprint).Should().BeFalse();
        Runtime.Registry.IsSigningKeyRetired(_original!.SigningKey.KeyId).Should().BeFalse();
    }

    [Then("目前配置的查詢仍回應 200")]
    public async Task ThenCurrentQueryStillOk()
    {
        var (status, body) = await QueryOrderAsync(_current!, _current!.SigningKey, _primaryOrderName);
        status.Should().Be(HttpStatusCode.OK, body);
    }

    [Then("既有配置的查詢於 60 秒內回應 401")]
    public async Task ThenOriginalQueryRejectedWithinThreshold()
    {
        var rejectedAt = await PollUntilRejectedAsync(() => QueryOrderAsync(_original!, _original!.SigningKey, _primaryOrderName));
        Measure("既有配置", rejectedAt);
    }

    [Then("目前配置的查詢於 60 秒內回應 401")]
    public async Task ThenCurrentQueryRejectedWithinThreshold()
    {
        var rejectedAt = await PollUntilRejectedAsync(() => QueryOrderAsync(_current!, _current!.SigningKey, _primaryOrderName));
        Measure("目前配置", rejectedAt);
    }

    public void Dispose()
    {
        _secondary?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private static (bool Certificate, bool Key) ParseKind(string kind) => kind switch
    {
        "mTLS 憑證" => (true, false),
        "簽章金鑰" => (false, true),
        "mTLS 憑證與簽章金鑰" => (true, true),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "未知的輪替對象"),
    };

    private SpikeRuntime RuntimeOf(string environmentName)
    {
        if (environmentName == Runtime.EnvironmentName)
        {
            return Runtime;
        }

        if (_secondary is not null && environmentName == _secondary.EnvironmentName)
        {
            return _secondary;
        }

        throw new ArgumentOutOfRangeException(nameof(environmentName), environmentName, "未啟動的執行環境");
    }

    private void Measure(string configuration, DateTimeOffset rejectedAt)
    {
        var elapsed = rejectedAt - _revokedAt!.Value;
        Console.WriteLine($"[08 測量] {configuration}：洩漏撤銷起點 {_revokedAt:O}，首次拒絕 {rejectedAt:O}，耗時 {elapsed.TotalSeconds:F2} 秒（門檻 {LeakThresholdDescription}）");
        elapsed.Should().BeLessThanOrEqualTo(RevocationThreshold);
    }

    private async Task<DateTimeOffset> PollUntilRejectedAsync(Func<Task<(HttpStatusCode Status, string Body)>> query)
    {
        var deadline = _revokedAt!.Value.Add(RevocationThreshold);
        while (true)
        {
            var (status, body) = await query();
            var now = DateTimeOffset.UtcNow;
            if (status == HttpStatusCode.Unauthorized)
            {
                return now;
            }

            if (now > deadline)
            {
                throw new InvalidOperationException($"撤銷後超過 60 秒仍未被拒絕，最後回應 {(int)status}：{body}");
            }

            await Task.Delay(250);
        }
    }

    private async Task CreateOrderAsync(Caller caller, string orderName)
    {
        var (status, body) = await CreateOrderRawAsync(caller);
        status.Should().Be(HttpStatusCode.Created, body);
        _lastStatus = status;
        _lastBody = body;
        using var document = JsonDocument.Parse(body);
        _orders[orderName] = document.RootElement.GetProperty("orderId").GetGuid();
    }

    private async Task<(HttpStatusCode Status, string Body)> CreateOrderRawAsync(Caller caller)
    {
        var now = DateTimeOffset.UtcNow;
        using var request = new HttpRequestMessage(HttpMethod.Post, OrdersUri(caller.Runtime.OrdersApi.Port, "orders"))
        {
            Content = new StringContent($$"""{"orderReference":"{{Guid.NewGuid():N}}","item":"book","quantity":1}""", Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString());
        await BusinessRequestSigner.SignAsync(request, caller.SigningKey, now, now.AddSeconds(60), NewNonce());
        return await SendAsync(request, caller);
    }

    private async Task<(HttpStatusCode Status, string Body)> QueryOrderAsync(Caller caller, SignatureKey key, string orderName)
    {
        var now = DateTimeOffset.UtcNow;
        using var request = new HttpRequestMessage(HttpMethod.Get, OrdersUri(caller.Runtime.OrdersApi.Port, $"orders/{_orders[orderName]}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);
        await BusinessRequestSigner.SignAsync(request, key, now, now.AddSeconds(60), NewNonce());
        return await SendAsync(request, caller);
    }

    private static async Task<(HttpStatusCode Status, string Body)> SendAsync(HttpRequestMessage request, Caller caller)
    {
        using var client = CreateClient(caller.Runtime, caller.Certificate);
        using var response = await client.SendAsync(request);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private static async Task<(bool Issued, string? Token)> RequestTokenAsync(SpikeRuntime runtime, X509Certificate2 certificate)
    {
        using var client = CreateClient(runtime, certificate);
        using var response = await client.PostAsync(
            new Uri(runtime.AuthServer.Issuer, "connect/token"),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = ClientId,
            }));

        if (!response.IsSuccessStatusCode)
        {
            return (false, null);
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (true, document.RootElement.GetProperty("access_token").GetString());
    }

    private static HttpClient CreateClient(SpikeRuntime runtime, X509Certificate2 certificate)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = runtime.Trust.ServerCertificateValidator,
            ClientCertificateOptions = ClientCertificateOption.Manual,
        };
        handler.ClientCertificates.Add(certificate);
        return new HttpClient(handler, disposeHandler: true);
    }

    private static string NewNonce() => Guid.NewGuid().ToString("N");

    private static Uri OrdersUri(int port, string path) => new($"https://localhost:{port}/{path}");

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

    private static string CheckboxAt(int index)
    {
        var checkboxes = IssueText.Split('\n')
            .Where(line => Regex.IsMatch(line, @"^- \[[ x]\] "))
            .ToList();
        checkboxes.Should().HaveCountGreaterThanOrEqualTo(index);
        return checkboxes[index - 1];
    }
}
