using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lab.Creds.Proof.Signatures;
using Lab.Creds.Proof.Tests.Support;
using Reqnroll;
using Xunit;

namespace Lab.Creds.Proof.Tests.Steps;

[Binding]
public sealed class SignatureSteps(ScenarioState state)
{
    private const string BusinessTarget = "/partner/inspect/orders?tenant=acme";
    private static readonly byte[] Body = Encoding.UTF8.GetBytes(SignedRequests.DemoBody);
    private static readonly string[] BodyComponents = ["@method", "@authority", "@path", "@query", "authorization", "content-type", "content-digest", "idempotency-key"];

    private HttpResponseMessage? _response;
    private string? _responseBody;
    private BusinessCallCounter? _counter;
    private int _before;
    private SignedCall? _lastSent;
    private int _lastStatus;
    private string? _lastBody;

    [Given("該服務另外取得第二個有效 token")]
    public async Task GivenSecondToken()
    {
        using var timeout = new CancellationTokenSource(ProofEnvironment.OperationTimeout);
        var cancellationToken = timeout.Token;
        using var client = ProofEnvironment.CreateClient(ProofEnvironment.AuthServerUri, state.ClientCertificate);
        using var response = await client.PostAsync("/connect/token", new FormUrlEncodedContent(
        [
            new("grant_type", "client_credentials"),
            new("client_id", state.ClientId!),
            new("scope", ProofDefaults.SubmitScope)
        ]), cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, json);
        using var document = JsonDocument.Parse(json);
        state.SecondAccessToken = document.RootElement.GetProperty("access_token").GetString();
        Assert.NotEqual(state.AccessToken, state.SecondAccessToken);
    }

    [When("該服務經 Gateway 送出 {string} 到 {string} 且 {string} 的合法簽章請求")]
    public async Task WhenValidShape(string method, string target, string bodyKind)
    {
        var body = bodyKind == "有本文" ? Body : null;
        var call = SignedRequests.Create(state.ClientId!, ProofEnvironment.GatewayUri, new HttpMethod(method), target, state.AccessToken, body);
        await Send(call, viaGateway: true);
    }

    [When("該服務經 Gateway 送出已簽章的 GET whoami 請求")]
    public async Task WhenWhoami()
    {
        var call = SignedRequests.Create(state.ClientId!, ProofEnvironment.GatewayUri, HttpMethod.Get, "/partner/whoami", state.AccessToken, null);
        await Send(call, viaGateway: true);
    }

    [When("該服務經 Gateway 送出簽章後被改為 {string} 的請求")]
    public async Task WhenTamperedViaGateway(string tamper) => await Send(Tamper(tamper, ProofEnvironment.GatewayUri), viaGateway: true);

    [When("該服務直接對 API 送出簽章後被改為 {string} 的請求")]
    public async Task WhenTamperedDirect(string tamper) => await Send(Tamper(tamper, ProofEnvironment.ApiUri), viaGateway: false);

    [When("該服務經 Gateway 重送完全相同的已簽章請求兩次")]
    public async Task WhenReplay()
    {
        var call = SignedRequests.Create(state.ClientId!, ProofEnvironment.GatewayUri, HttpMethod.Post, BusinessTarget, state.AccessToken, Body);
        await Send(call, viaGateway: true);
        var first = _lastStatus;
        await Send(call, viaGateway: true, resetCounter: false);
        _replayStatuses = [first, _lastStatus];
    }

    private int[] _replayStatuses = [];

    [When("該服務直接對 API 送出 {string} 的 chunked 簽章請求")]
    public async Task WhenChunked(string shape)
    {
        var baseAddress = ProofEnvironment.ApiUri;
        SignedCall call;
        switch (shape)
        {
            case "非空本文並依有本文簽署":
                call = SignedRequests.Create(state.ClientId!, baseAddress, HttpMethod.Post, BusinessTarget, state.AccessToken, Body);
                break;
            case "空本文並依無本文簽署":
                call = SignedRequests.Create(state.ClientId!, baseAddress, HttpMethod.Post, BusinessTarget, state.AccessToken, []);
                break;
            case "非空本文但依無本文簽署":
                call = SignedRequests.Create(state.ClientId!, baseAddress, HttpMethod.Post, BusinessTarget, state.AccessToken, null);
                call.Body = Body;
                call.ContentType = "application/json";
                break;
            case "空本文但依有本文簽署":
                call = SignedRequests.Create(state.ClientId!, baseAddress, HttpMethod.Post, BusinessTarget, state.AccessToken, Body);
                call.Body = [];
                break;
            default:
                throw new ArgumentException(shape);
        }

        call.Chunked = true;
        await Send(call, viaGateway: false);
    }

    [Then("API 回應 200 且驗證的簽章金鑰為 {string}")]
    public void ThenAccepted(string keyId)
    {
        Assert.True(_lastStatus == 200, $"{_lastStatus} {_lastBody}");
        using var document = JsonDocument.Parse(_lastBody!);
        Assert.Equal(keyId, document.RootElement.GetProperty("verifiedKeyId").GetString());
    }

    [Then("API 回應 200 且回報 Client 為 {string}")]
    public void ThenWhoami(string clientId)
    {
        Assert.True(_lastStatus == 200, $"{_lastStatus} {_lastBody}");
        using var document = JsonDocument.Parse(_lastBody!);
        Assert.Equal(clientId, document.RootElement.GetProperty("clientId").GetString());
    }

    [Then("API 回應 {int} 且原因為 {string}")]
    public void ThenRejected(int status, string reason)
    {
        Assert.True(_lastStatus == status, $"{_lastStatus} {_lastBody}");
        if (status == 200 || reason == "none") return;
        using var document = JsonDocument.Parse(_lastBody!);
        Assert.Equal("invalid_request_signature", document.RootElement.GetProperty("error").GetString());
        Assert.Equal(reason, document.RootElement.GetProperty("reason").GetString());
        Assert.Equal("Bearer", _response!.Headers.WwwAuthenticate.Single().Scheme);
    }

    [Then("兩次回應皆為 200")]
    public void ThenBothAccepted() => Assert.Equal([200, 200], _replayStatuses);

    [Then("業務處理未被執行")]
    public void ThenNotExecuted() => Assert.Equal(_before, _counter!.Count);

    [Then("業務處理被執行 {int} 次")]
    public void ThenExecuted(int times) => Assert.Equal(_before + times, _counter!.Count);

    [Then("日誌只記錄 keyid 與原因碼而不含 token、Authorization、簽章值與 signature base")]
    public void ThenLogsClean()
    {
        var entries = ProofEnvironment.Logs.Entries.ToArray();
        var rejections = entries.Where(e => e.Contains("signature_invalid")).ToArray();
        Assert.NotEmpty(rejections);
        Assert.Contains(rejections, e => e.Contains(state.ClientId + "-sig-1"));
        var signatureValue = _lastSent!.Signature!.Split(':')[1];
        foreach (var secret in new[] { state.AccessToken!, _lastSent.Authorization!, signatureValue, "@signature-params", "Signature-Input" })
        {
            Assert.DoesNotContain(entries, e => e.Contains(secret, StringComparison.Ordinal));
        }
    }

    private async Task Send(SignedCall call, bool viaGateway, bool resetCounter = true)
    {
        using var timeout = new CancellationTokenSource(ProofEnvironment.OperationTimeout);
        var cancellationToken = timeout.Token;

        if (resetCounter)
        {
            _counter = viaGateway ? ProofEnvironment.GatewayApiBusinessCalls : ProofEnvironment.ApiBusinessCalls;
            _before = _counter.Count;
        }

        using var client = ProofEnvironment.CreateClient(viaGateway ? ProofEnvironment.GatewayUri : ProofEnvironment.ApiUri, state.ClientCertificate);
        using var request = call.Build();
        _response?.Dispose();
        _response = await client.SendAsync(request, cancellationToken);
        _responseBody = await _response.Content.ReadAsStringAsync(cancellationToken);
        _lastSent = call;
        _lastStatus = (int)_response.StatusCode;
        _lastBody = _responseBody;
    }

    // Builds a request that is valid in every respect except the one named defect, so a rejection can only be
    // caused by that defect (valid certificate, valid unexpired token, correct Client).
    private SignedCall Tamper(string tamper, Uri baseAddress)
    {
        var token = state.AccessToken;
        var own = SignedRequests.KeyOf(state.ClientId!);
        var other = state.ClientId == "partner-a" ? ProofEnvironment.PartnerBSigningKey : ProofEnvironment.PartnerASigningKey;
        SignedCall Sign(SignOptions? options = null, TestSigningKey? key = null, string? target = null, byte[]? body = null, HttpMethod? method = null)
            => RequestSigner.Sign(method ?? HttpMethod.Post, baseAddress.Authority, target ?? BusinessTarget, token, body ?? Body, key ?? own, options);

        SignedCall call;
        switch (tamper)
        {
            case "缺少簽章標頭":
                call = Sign();
                call.SignatureInput = null;
                call.Signature = null;
                return call;
            case "只有 Signature-Input 沒有 Signature":
                call = Sign();
                call.Signature = null;
                return call;
            case "簽章值被改動":
                call = Sign();
                call.Signature = Rewrite(call.Signature!, bytes => bytes[10] ^= 0x01);
                return call;
            case "簽章長度不是 64 bytes":
                call = Sign();
                call.Signature = Rewrite(call.Signature!, bytes => bytes[..63]);
                return call;
            case "使用未登記的金鑰": return Sign(key: ProofEnvironment.UnregisteredSigningKey);
            case "使用另一個 Client 的合法金鑰": return Sign(key: other);
            case "使用已停用的金鑰": return Sign(key: ProofEnvironment.PartnerADisabledSigningKey);
            case "登記的 keyid 但使用不同私鑰": return Sign(new SignOptions { KeyId = own.KeyId }, ProofEnvironment.UnregisteredSigningKey);
            case "演算法不是 ecdsa-p256-sha256": return Sign(new SignOptions { Algorithm = "ecdsa-p384-sha384" });
            case "缺少 nonce 參數": return Sign(new SignOptions { ParameterOrder = ["created", "expires", "keyid", "alg"] });
            case "參數順序錯誤": return Sign(new SignOptions { ParameterOrder = ["created", "expires", "keyid", "nonce", "alg"] });
            case "簽章標籤不是 sig1": return Sign(new SignOptions { Label = "sig2" });
            case "覆蓋元件缺少 authorization": return Sign(new SignOptions { Components = BodyComponents.Where(c => c != "authorization").ToArray() });
            case "覆蓋元件順序錯誤": return Sign(new SignOptions { Components = ["@method", "@path", "@authority", .. BodyComponents.Skip(3)] });
            case "覆蓋元件多出 @scheme": return Sign(new SignOptions { Components = [.. BodyComponents, "@scheme"] });
            case "簽章已過期": return Sign(new SignOptions { Now = DateTimeOffset.UtcNow.AddSeconds(-200) });
            case "簽章尚未生效": return Sign(new SignOptions { Now = DateTimeOffset.UtcNow.AddSeconds(200) });
            case "有效期超過 60 秒":
                var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return Sign(new SignOptions { Created = created, Expires = created + 61 });
            case "nonce 格式錯誤": return Sign(new SignOptions { Nonce = "short" });
            case "本文被竄改":
                call = Sign();
                call.Body = Encoding.UTF8.GetBytes("""{"partnerName":"Evil","payload":"demo"}""");
                return call;
            case "Content-Digest 使用 sha-512":
                call = Sign();
                call.ContentDigest = "sha-512=:" + Convert.ToBase64String(SHA512.HashData(Body)) + ":";
                return call;
            case "移除 Content-Digest":
                call = Sign();
                call.ContentDigest = null;
                return call;
            case "查詢參數被竄改":
                call = Sign();
                call.PathAndQuery = "/partner/inspect/orders?tenant=evil";
                return call;
            case "路徑被替換":
                call = Sign();
                call.PathAndQuery = "/partner/inspect/other?tenant=acme";
                return call;
            case "方法被替換":
                call = Sign();
                call.Method = HttpMethod.Put;
                return call;
            case "authority 被替換": return Sign(new SignOptions { Authority = "evil.example:443" });
            case "Authorization 被換成另一個有效 token":
                call = Sign();
                call.Authorization = "Bearer " + state.SecondAccessToken;
                return call;
            case "Idempotency-Key 被替換":
                call = Sign();
                call.IdempotencyKey = "replaced-key";
                return call;
            case "Content-Type 被替換":
                call = Sign();
                call.ContentType = "text/plain";
                return call;
            case "移除 Idempotency-Key":
                call = Sign();
                call.IdempotencyKey = null;
                return call;
            case "GET 帶有本文":
                call = Sign(method: HttpMethod.Get, options: new SignOptions { Components = ["@method", "@authority", "@path", "@query", "authorization"] });
                return call;
            default:
                throw new ArgumentException(tamper);
        }
    }

    private static string Rewrite(string signatureHeader, Action<byte[]> mutate) => Rewrite(signatureHeader, bytes => { mutate(bytes); return bytes; });

    private static string Rewrite(string signatureHeader, Func<byte[], byte[]> mutate)
    {
        var parts = signatureHeader.Split(':');
        return $"{parts[0]}:{Convert.ToBase64String(mutate(Convert.FromBase64String(parts[1])))}:";
    }
}
