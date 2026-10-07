using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using Lab.Creds.Proof.Tests.Support;
using Reqnroll;
using Xunit;

namespace Lab.Creds.Proof.Tests.Steps;

[Binding]
public sealed class GatewaySteps(ScenarioState state)
{
    private HttpResponseMessage? _response;
    private string? _body;
    private HttpRequestException? _failure;

    [When("該服務以同一憑證和 token 經 Gateway 對 API 提交合作廠商資料")]
    public Task WhenSameCertificateViaGateway() => Submit(state.ClientCertificate, state.AccessToken);

    [When("攻擊者只帶 token 且不出示憑證經 Gateway 提交合作廠商資料")]
    public Task WhenNoCertificateViaGateway() => Submit(null, state.AccessToken);

    [When("持有未註冊憑證的呼叫端帶著 partner-a 的 token 經 Gateway 提交合作廠商資料")]
    public Task WhenStrangerViaGateway() => Submit(ProofEnvironment.Stranger, state.AccessToken);

    [When("持有 {string} 憑證的呼叫端帶著 partner-a 的 token 經 Gateway 提交合作廠商資料")]
    public Task WhenOtherRegisteredViaGateway(string clientId)
        => Submit(clientId == "partner-b" ? ProofEnvironment.PartnerB : throw new ArgumentException(clientId), state.AccessToken);

    [When("持有 {string} 憑證的呼叫端帶著 partner-a 的 token 並偽造聲稱為 partner-a 憑證的轉送標頭經 Gateway 提交合作廠商資料")]
    public Task WhenForgedForwardedCertificate(string clientId)
    {
        var forgedPem = Uri.EscapeDataString(ProofEnvironment.PartnerA.ExportCertificatePem());
        return Submit(
            clientId == "partner-b" ? ProofEnvironment.PartnerB : throw new ArgumentException(clientId),
            state.AccessToken,
            new Dictionary<string, string>
            {
                ["X-Forwarded-Client-Cert"] = $"Cert=\"{forgedPem}\"",
                ["X-Client-Cert-SHA256"] = ProofEnvironment.PartnerA.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256)
            });
    }

    [When("受信 Gateway 測試憑證直連 API 並帶上 {string} 的轉送憑證標頭")]
    public async Task WhenTrustedGatewayWithXfcc(string scenario)
    {
        var good = $"Cert=\"{Uri.EscapeDataString(ProofEnvironment.PartnerA.ExportCertificatePem())}\"";
        var other = $"Cert=\"{Uri.EscapeDataString(ProofEnvironment.PartnerB.ExportCertificatePem())}\"";
        string[] values = scenario switch
        {
            "無標頭" => [],
            "非 PEM 內容" => ["Cert=\"not-a-certificate\""],
            "損毀的 PEM" => ["Cert=\"" + Uri.EscapeDataString("-----BEGIN CERTIFICATE-----\nAAAA\n-----END CERTIFICATE-----") + "\""],
            "缺少 Cert 欄位" => ["By=spiffe://x;Hash=abc"],
            "重複的標頭" => [good, good],
            "多個 Cert 項目" => [good + "," + other],
            _ => throw new ArgumentException(scenario)
        };

        using var client = ProofEnvironment.CreateClient(ProofEnvironment.GatewayApiUri, ProofEnvironment.Certificates.Gateway);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/partner/submissions")
        {
            Content = new StringContent("{\"partnerName\":\"Acme\",\"payload\":\"demo\"}", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", state.AccessToken);
        if (values.Length > 0)
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-Client-Cert", values);
        }

        _response = await client.SendAsync(request);
        _body = await _response.Content.ReadAsStringAsync();
    }

    [Then("Gateway 後的 API 回應 401 且不接受請求")]
    public void ThenUnauthorizedNoLeak()
    {
        Assert.True(_response!.StatusCode == HttpStatusCode.Unauthorized, $"{(int)_response.StatusCode} {_body}");
        Assert.DoesNotContain("partner-a", _body!);
        Assert.DoesNotContain("BEGIN CERTIFICATE", _body!);
    }

    [When("該服務以同一憑證和 token 繞過 Gateway 直連 API 提交合作廠商資料")]
    public Task WhenBypassGateway() => Submit(state.ClientCertificate, state.AccessToken, baseAddress: ProofEnvironment.GatewayApiUri);

    [When("持有非受信 Gateway 憑證的呼叫端偽造 partner-a 憑證的轉送標頭直連 API 提交合作廠商資料")]
    public Task WhenRogueGateway()
        => Submit(
            ProofEnvironment.Certificates.UntrustedGateway,
            state.AccessToken,
            new Dictionary<string, string>
            {
                ["X-Forwarded-Client-Cert"] = $"Cert=\"{Uri.EscapeDataString(ProofEnvironment.PartnerA.ExportCertificatePem())}\""
            },
            ProofEnvironment.GatewayApiUri);

    [Then("API 拒絕該連線")]
    public void ThenRejectedByApi()
    {
        Assert.True(_failure is not null, $"{_response?.StatusCode} {_body}");
        Assert.Null(_response);
    }

    [When("該服務以同一憑證和 token 經 Gateway 查詢 API 所見的請求資訊")]
    public async Task WhenInspect()
    {
        await Inspect(state.ClientCertificate, state.AccessToken, "/partner/inspect/plain", new Dictionary<string, string>(), []);
        Assert.True(_response?.StatusCode == HttpStatusCode.OK, $"{_failure} {_response?.StatusCode} {_body}");
    }

    [Then("API 所見的 Client 憑證 x5t#S256 等於 token 的 cnf 與 partner-a 憑證指紋")]
    public async Task ThenCertificateMatchesCnf()
    {
        using var json = JsonDocument.Parse(_body!);
        var observed = json.RootElement.GetProperty("clientCertificateX5tS256").GetString();

        using var authServer = ProofEnvironment.CreateClient(ProofEnvironment.AuthServerUri, ProofEnvironment.ApiIdentity);
        var introspection = await authServer.PostAsync("/connect/introspect", new FormUrlEncodedContent(
            [new("client_id", ProofDefaults.Audience), new("token", state.AccessToken!)]));
        using var introspected = JsonDocument.Parse(await introspection.Content.ReadAsStringAsync());
        var cnf = introspected.RootElement.GetProperty("cnf").GetProperty("x5t#S256").GetString();

        Assert.Equal(cnf, observed);
        Assert.Equal(Base64UrlEncoder.Encode(SHA256.HashData(ProofEnvironment.PartnerA.RawData)), observed);
    }

    [Then("API 所見的 Client 憑證 x5t#S256 不等於 Gateway 憑證指紋")]
    public void ThenNotGatewayCertificate()
    {
        using var json = JsonDocument.Parse(_body!);
        var observed = json.RootElement.GetProperty("clientCertificateX5tS256").GetString();
        Assert.NotEqual(Base64UrlEncoder.Encode(SHA256.HashData(ProofEnvironment.Certificates.Gateway.RawData)), observed);
    }

    private const string RawPathAndQuery = "/partner/inspect/a%2Fb//c?x=1&y=%E4%B8%AD&x=2";
    private static readonly byte[] SignedBody = Encoding.UTF8.GetBytes("{\"partnerName\":\"Acme\",\"payload\":\"中文 body\"}");
    private static readonly Dictionary<string, string> SignatureHeaders = new()
    {
        ["Content-Digest"] = "sha-256=:X48E9qOokqqrvdts8nOJRJN3OWDUoyWxBf7kbu9DBPE=:",
        ["Signature-Input"] = "sig1=(\"@method\" \"@authority\" \"@path\" \"@query\" \"content-digest\");created=1700000000;keyid=\"partner-a\"",
        ["Signature"] = "sig1=:dGVzdC1zaWduYXR1cmUtbm90LXZlcmlmaWVkLWluLXRoaXMtdGlja2V0:"
    };

    [When("該服務經 Gateway 提交帶有簽章相關標頭的請求")]
    public Task WhenSubmitSignatureHeaders()
        => Inspect(state.ClientCertificate, state.AccessToken, RawPathAndQuery, SignatureHeaders, SignedBody);

    [Then("API 所見的 authority、原始路徑與查詢、本文雜湊與簽章相關標頭與送出時完全相同")]
    public void ThenPreserved()
    {
        Assert.True(_response?.StatusCode == HttpStatusCode.OK, $"{_failure} {_response?.StatusCode} {_body}");
        using var json = JsonDocument.Parse(_body!);
        var root = json.RootElement;
        Assert.Equal($"localhost:{ProofEnvironment.GatewayUri.Port}", root.GetProperty("host").GetString());
        Assert.Equal(RawPathAndQuery, root.GetProperty("rawTarget").GetString());
        Assert.Equal(Base64UrlEncoder.Encode(SHA256.HashData(SignedBody)), root.GetProperty("bodySha256").GetString());
        foreach (var (name, value) in SignatureHeaders)
        {
            Assert.Equal(value, root.GetProperty("headers").GetProperty(name.ToLowerInvariant()).GetString());
        }
    }

    private async Task Inspect(
        X509Certificate2? certificate, string? accessToken, string rawPathAndQuery,
        IDictionary<string, string> headers, byte[] body)
    {
        using var client = ProofEnvironment.CreateClient(ProofEnvironment.GatewayUri, certificate);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(rawPathAndQuery, UriKind.Relative))
        {
            Content = new ByteArrayContent(body)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        foreach (var (name, value) in headers)
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        try
        {
            _response = await client.SendAsync(request);
            _body = await _response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException exception)
        {
            _failure = exception;
        }
    }

    [Then("連線在到達 API 之前即被 Gateway 拒絕")]
    public void ThenRejectedAtGateway()
    {
        Assert.True(_failure is not null, $"{_response?.StatusCode} {_body}");
        Assert.Null(_response);
    }

    [Then("Gateway 後的 API 回應 {int}")]
    public void ThenStatus(int status) => Assert.True((int)_response!.StatusCode == status, $"{_failure} {_response.StatusCode} {_body}");

    [Then("Gateway 後的 API 回應 202 並回報已驗證 Client 為 {string}")]
    public void ThenAccepted(string clientId)
    {
        Assert.True(_response!.StatusCode == HttpStatusCode.Accepted, _body);
        using var json = JsonDocument.Parse(_body!);
        Assert.Equal(clientId, json.RootElement.GetProperty("clientId").GetString());
    }

    private async Task Submit(X509Certificate2? certificate, string? accessToken, IDictionary<string, string>? extraHeaders = null, Uri? baseAddress = null)
    {
        using var client = ProofEnvironment.CreateClient(baseAddress ?? ProofEnvironment.GatewayUri, certificate);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/partner/submissions")
        {
            Content = new StringContent("""{"partnerName":"Acme","payload":"demo"}""", Encoding.UTF8, "application/json")
        };
        foreach (var (name, value) in extraHeaders ?? new Dictionary<string, string>())
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        try
        {
            _response = await client.SendAsync(request);
            _body = await _response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException exception)
        {
            _failure = exception;
        }
    }
}
