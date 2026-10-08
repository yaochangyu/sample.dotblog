using System.Security.Cryptography;
using System.Text;
using Lab.Creds.Proof.Signatures;
using Lab.Creds.Proof.Tests.Support;
using Xunit;

namespace Lab.Creds.Proof.Tests.Unit;

// RFC 9421 Appendix B.2.4 (ecdsa-p256-sha256, key test-key-ecc-p256 from B.1.3) is the interoperability anchor:
// the signature in the RFC was produced by someone else's signer, so passing it is not self-consistency.
public class Rfc9421InteropTests
{
    private const string PublicKeyPem = """
        -----BEGIN PUBLIC KEY-----
        MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEqIVYZVLCrPZHGHjP17CTW0/+D9Lf
        w0EkjqF7xB4FivAxzic30tMM4GF+hR6Dxh71Z50VGGdldkkDXZCnTNnoXQ==
        -----END PUBLIC KEY-----
        """;

    private const string SignatureInput =
        "sig-b24=(\"@status\" \"content-type\" \"content-digest\" \"content-length\");created=1618884473;keyid=\"test-key-ecc-p256\"";

    private const string Signature =
        "sig-b24=:wNmSUAhwb5LxtOtOpNa6W5xj067m5hFrj0XQ4fvpaCLx0NKocgPquLgyahnzDnDAUy5eCdlYUEkLIj+32oiasw==:";

    private static readonly Dictionary<string, string> Response = new()
    {
        ["@status"] = "200",
        ["content-type"] = "application/json",
        ["content-digest"] = "sha-512=:mEWXIS7MaLRuGgxOBdODa3xqM1XdEvxoYhvlCFJ41QJgJc4GTsPp29l5oGX69wWdXymyU0rjJuahq4l5aGgfLQ==:",
        ["content-length"] = "23"
    };

    private static byte[] PublicKey()
    {
        using var key = ECDsa.Create();
        key.ImportFromPem(PublicKeyPem);
        return key.ExportSubjectPublicKeyInfo();
    }

    private static string Base(Dictionary<string, string> values)
    {
        Assert.True(StructuredFieldParser.TryParseSignatureInput(SignatureInput, out var input));
        return HttpMessageSignature.BuildSignatureBase(input, name => values[name]);
    }

    private static byte[] RfcSignature()
    {
        Assert.True(StructuredFieldParser.TryParseSignature(Signature, out var label, out var bytes));
        Assert.Equal("sig-b24", label);
        return bytes;
    }

    [Fact]
    public void Given_RFC9421_B24_的簽章基底_When_組裝_Then_與RFC列出的文字完全相同()
    {
        const string expected =
            "\"@status\": 200\n" +
            "\"content-type\": application/json\n" +
            "\"content-digest\": sha-512=:mEWXIS7MaLRuGgxOBdODa3xqM1XdEvxoYhvlCFJ41QJgJc4GTsPp29l5oGX69wWdXymyU0rjJuahq4l5aGgfLQ==:\n" +
            "\"content-length\": 23\n" +
            "\"@signature-params\": (\"@status\" \"content-type\" \"content-digest\" \"content-length\");created=1618884473;keyid=\"test-key-ecc-p256\"";

        Assert.Equal(expected, Base(Response));
    }

    [Fact]
    public void Given_RFC9421_B24_的簽章與B13公鑰_When_驗證_Then_通過()
    {
        Assert.Equal(64, RfcSignature().Length);
        Assert.True(HttpMessageSignature.VerifyEcdsaP256(PublicKey(), Base(Response), RfcSignature()));
    }

    [Fact]
    public void Given_RFC9421_B24_任一被簽元件被改動_When_驗證_Then_失敗()
    {
        var tampered = new Dictionary<string, string>(Response) { ["content-length"] = "24" };
        Assert.False(HttpMessageSignature.VerifyEcdsaP256(PublicKey(), Base(tampered), RfcSignature()));
    }

    [Fact]
    public void Given_RFC9421_B24_簽章被改動一個位元_When_驗證_Then_失敗()
    {
        var signature = RfcSignature();
        signature[10] ^= 0x01;
        Assert.False(HttpMessageSignature.VerifyEcdsaP256(PublicKey(), Base(Response), signature));
    }

    [Fact]
    public void Given_RFC9421_B24_簽章與另一把公鑰_When_驗證_Then_失敗()
    {
        using var other = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        Assert.False(HttpMessageSignature.VerifyEcdsaP256(other.ExportSubjectPublicKeyInfo(), Base(Response), RfcSignature()));
    }
}

public class StructuredFieldParserTests
{
    [Fact]
    public void Given_引號字串內含逗號分號等號與跳脫_When_解析_Then_不被當成分隔符()
    {
        Assert.True(StructuredFieldParser.TryParseSignatureInput(
            "sig1=(\"@method\");nonce=\"a,b;c=d\";keyid=\"k\\\"1\"", out var parsed));

        Assert.Equal(["@method"], parsed.Components);
        Assert.Equal(new SignatureParameter("nonce", "a,b;c=d"), parsed.Parameters[0]);
        Assert.Equal(new SignatureParameter("keyid", "k\"1"), parsed.Parameters[1]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sig1=(\"@method\");created=1;created=2")]
    [InlineData("sig1=(\"@method\"), sig2=(\"@method\")")]
    [InlineData("sig1=(\"@method\"),sig2=(\"@method\")")]
    [InlineData("sig1=(\"@method\"  \"@path\")")]
    [InlineData("sig1=(\"@method\" )")]
    [InlineData("sig1=( \"@method\")")]
    [InlineData("sig1=(\"@method\") ")]
    [InlineData("sig1=(\"@method\");created=1.5")]
    [InlineData("sig1=(\"@method\");created=0x10")]
    [InlineData("sig1=(\"@method\");created=1234567890123456")]
    [InlineData("sig1=(\"@method\");nonce=abc")]
    [InlineData("sig1=(\"@method\");keyid=\"a\\x\"")]
    [InlineData("sig1=(\"@method\");keyid=\"unterminated")]
    [InlineData("SIG1=(\"@method\")")]
    [InlineData("sig1=(@method)")]
    [InlineData("sig1=(\"@method\";name=\"x\")")]
    [InlineData("sig1=(\"@method\");created")]
    [InlineData("sig1=(\"@method\");created=1;")]
    [InlineData("sig1 =(\"@method\")")]
    [InlineData("sig1=(\"@method\")\n")]
    public void Given_不在嚴格子集內或有歧義的Signature_Input_When_解析_Then_拒絕(string value)
        => Assert.False(StructuredFieldParser.TryParseSignatureInput(value, out _));

    [Theory]
    [InlineData("sig1=:YWJj:")]
    [InlineData("sig1=:YWJjZA==:")]
    public void Given_合法的Signature_When_解析_Then_取得label與位元組(string value)
    {
        Assert.True(StructuredFieldParser.TryParseSignature(value, out var label, out var bytes));
        Assert.Equal("sig1", label);
        Assert.NotEmpty(bytes);
    }

    [Theory]
    [InlineData("sig1=:YWJj:, sig2=:ZGVm:")]
    [InlineData("sig1=:!!!!:")]
    [InlineData("sig1=YWJj")]
    [InlineData("sig1=:YWJj")]
    [InlineData("sig1=:YWJj:x")]
    [InlineData("sig1=:YW Jj:")]
    [InlineData("sig1=:YWJ:")]
    [InlineData("")]
    public void Given_有歧義或格式錯誤的Signature_When_解析_Then_拒絕(string value)
        => Assert.False(StructuredFieldParser.TryParseSignature(value, out _, out _));

    [Theory]
    [InlineData("sha-256=:YWJj:", 1)]
    [InlineData("sha-256=:YWJj:, sha-512=:ZGVm:", 2)]
    [InlineData("sha-256=:YWJj:,sha-512=:ZGVm:", 2)]
    public void Given_合法的Content_Digest字典_When_解析_Then_取得所有成員(string value, int count)
    {
        Assert.True(StructuredFieldParser.TryParseDigestDictionary(value, out var members));
        Assert.Equal(count, members.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sha-256=YWJj")]
    [InlineData("sha-256=:YWJj:,")]
    [InlineData("sha-256=:YWJj:sha-512=:ZGVm:")]
    [InlineData("sha-256=:YWJj:, sha-256=:ZGVm:")]
    [InlineData("sha-256=\"YWJj\"")]
    [InlineData("sha-256=:YWJj:;a=1")]
    [InlineData("SHA-256=:YWJj:")]
    public void Given_有歧義或格式錯誤的Content_Digest_When_解析_Then_拒絕(string value)
        => Assert.False(StructuredFieldParser.TryParseDigestDictionary(value, out _));
}

public class LabSignatureProfileTests : IDisposable
{
    private const string Authority = "localhost:8443";
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
    private static readonly byte[] Json = Encoding.UTF8.GetBytes("""{"partnerName":"Acme"}""");
    private readonly TestSigningKey _partnerA = new("partner-a-sig-1");
    private readonly TestSigningKey _partnerB = new("partner-b-sig-1");

    public void Dispose()
    {
        _partnerA.Dispose();
        _partnerB.Dispose();
    }

    private ValueTask<RegisteredSigningKey?> Lookup(string keyId, CancellationToken _)
        => ValueTask.FromResult<RegisteredSigningKey?>(keyId switch
        {
            "partner-a-sig-1" => new RegisteredSigningKey { KeyId = keyId, ClientId = "partner-a", Algorithm = LabSignatureProfile.Algorithm, PublicKey = _partnerA.PublicKey },
            "partner-b-sig-1" => new RegisteredSigningKey { KeyId = keyId, ClientId = "partner-b", Algorithm = LabSignatureProfile.Algorithm, PublicKey = _partnerB.PublicKey },
            "partner-a-disabled" => new RegisteredSigningKey { KeyId = keyId, ClientId = "partner-a", Algorithm = LabSignatureProfile.Algorithm, PublicKey = _partnerA.PublicKey, IsActive = false },
            "partner-a-other-alg" => new RegisteredSigningKey { KeyId = keyId, ClientId = "partner-a", Algorithm = "ed25519", PublicKey = _partnerA.PublicKey },
            _ => null
        });

    private Task<SignatureVerification> Verify(SignatureRequestView view, string client = "partner-a")
        => LabSignatureProfile.VerifyAsync(view, client, Now, Lookup, CancellationToken.None);

    private SignedCall Sign(HttpMethod method, string target, byte[]? body, SignOptions? options = null, TestSigningKey? key = null)
        => RequestSigner.Sign(method, Authority, target, "token-value", body, key ?? _partnerA, options ?? new SignOptions { Now = Now });

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task Given_讀取且無Body的合法簽章_When_驗證_Then_通過(string method)
    {
        var result = await Verify(Sign(new HttpMethod(method), "/partner/whoami", null).ToView());
        Assert.True(result.Succeeded, result.Reason);
        Assert.Equal("partner-a-sig-1", result.KeyId);
    }

    [Theory]
    [InlineData("POST", false)]
    [InlineData("DELETE", false)]
    [InlineData("POST", true)]
    [InlineData("PUT", true)]
    [InlineData("PATCH", true)]
    [InlineData("DELETE", true)]
    public async Task Given_有副作用的合法簽章_When_驗證_Then_通過(string method, bool hasBody)
    {
        var result = await Verify(Sign(new HttpMethod(method), "/partner/x?a=1", hasBody ? Json : null).ToView());
        Assert.True(result.Succeeded, result.Reason);
    }

    [Theory]
    [InlineData("/partner/x", "?")]
    [InlineData("/partner/x?", "?")]
    [InlineData("/partner/a%2Fb//c?x=1&y=%E4%B8%AD&x=2", "?x=1&y=%E4%B8%AD&x=2")]
    public async Task Given_各種原始target_When_驗證_Then_以原始path與query計算且空query為單獨問號(string target, string expectedQuery)
    {
        var call = Sign(HttpMethod.Get, target, null);
        Assert.Equal(expectedQuery, call.ToView().Query);
        Assert.True((await Verify(call.ToView())).Succeeded);
    }

    [Theory]
    [InlineData("GET", true, "body_not_allowed")]
    [InlineData("HEAD", true, "body_not_allowed")]
    [InlineData("OPTIONS", false, "method_unsupported")]
    public async Task Given_不合法的方法與Body組合_When_驗證_Then_拒絕(string method, bool hasBody, string reason)
    {
        var call = Sign(new HttpMethod(method), "/p", null);
        var view = call.ToView() with { Body = hasBody ? Json : ReadOnlyMemory<byte>.Empty };
        Assert.Equal(reason, (await Verify(view)).Reason);
    }

    [Fact]
    public async Task Given_實際有Body但簽章宣告為無Body清單_When_驗證_Then_拒絕()
    {
        var view = Sign(HttpMethod.Post, "/p", null).ToView() with { Body = Json };
        Assert.Equal("components_mismatch", (await Verify(view)).Reason);
    }

    [Fact]
    public async Task Given_實際無Body但簽章宣告為有Body清單_When_驗證_Then_拒絕()
    {
        var view = Sign(HttpMethod.Post, "/p", Json).ToView() with { Body = ReadOnlyMemory<byte>.Empty };
        Assert.Equal("components_mismatch", (await Verify(view)).Reason);
    }

    [Theory]
    [InlineData("@authority,@method,@path,@query,authorization,idempotency-key")]
    [InlineData("@method,@authority,@path,authorization,idempotency-key")]
    [InlineData("@method,@authority,@path,@query,idempotency-key")]
    [InlineData("@method,@authority,@path,@query,authorization")]
    [InlineData("@method,@authority,@scheme,@path,@query,authorization,idempotency-key")]
    [InlineData("@method,@authority,@path,@query,authorization,idempotency-key,content-type")]
    public async Task Given_元件順序錯誤_缺漏或多出_When_驗證_Then_拒絕(string components)
    {
        var call = Sign(HttpMethod.Post, "/p", null, new SignOptions { Now = Now, Components = components.Split(',') });
        Assert.Equal("components_mismatch", (await Verify(call.ToView())).Reason);
    }

    [Theory]
    [InlineData(30, 60, null)]
    [InlineData(31, 60, "signature_not_yet_valid")]
    [InlineData(0, 60, null)]
    [InlineData(-90, 60, null)]
    [InlineData(-91, 60, "signature_expired")]
    [InlineData(0, 1, null)]
    [InlineData(0, 61, "signature_time_invalid")]
    [InlineData(0, 0, "signature_time_invalid")]
    [InlineData(0, -1, "signature_time_invalid")]
    public async Task Given_created與expires相對現在的位置_When_驗證_Then_依120秒區間與60秒上限判斷(long createdOffset, long lifetime, string? reason)
    {
        var created = Now.ToUnixTimeSeconds() + createdOffset;
        var call = Sign(HttpMethod.Get, "/p", null, new SignOptions { Created = created, Expires = created + lifetime });
        var result = await Verify(call.ToView());
        Assert.Equal(reason, result.Reason);
        Assert.Equal(reason is null, result.Succeeded);
    }

    [Theory]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA", true)]
    [InlineData("_-_-_-_-_-_-_-_-_-_-_w", true)]
    [InlineData("AAAAAAAAAAAAAAAAAAAAA", false)]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAA", false)]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA==", false)]
    [InlineData("AAAAAAAAAAAAAAAAAAAAA+", false)]
    [InlineData("AAAAAAAAAAAAAAAAAAAAA/", false)]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAB", false)]
    [InlineData("AAAAAAAAAAAAAAAA AAAAA", false)]
    [InlineData("", false)]
    public void Given_nonce字串_When_檢查格式_Then_僅接受22字元無padding_base64url且為16位元組(string nonce, bool valid)
        => Assert.Equal(valid, LabSignatureProfile.IsValidNonce(nonce));

    [Fact]
    public async Task Given_nonce格式錯誤_When_驗證_Then_拒絕()
    {
        var call = Sign(HttpMethod.Get, "/p", null, new SignOptions { Now = Now, Nonce = "short" });
        Assert.Equal("nonce_invalid", (await Verify(call.ToView())).Reason);
    }

    [Theory]
    [InlineData("partner-unknown-key", "key_unknown")]
    [InlineData("partner-b-sig-1", "key_client_mismatch")]
    [InlineData("partner-a-disabled", "key_inactive")]
    [InlineData("partner-a-other-alg", "algorithm_unsupported")]
    public async Task Given_金鑰登記狀態不符_When_驗證_Then_拒絕(string keyId, string reason)
    {
        var call = Sign(HttpMethod.Get, "/p", null, new SignOptions { Now = Now, KeyId = keyId });
        Assert.Equal(reason, (await Verify(call.ToView())).Reason);
    }

    [Fact]
    public async Task Given_以另一Client的金鑰簽署並使用該keyid_When_以partner_a的token驗證_Then_拒絕()
    {
        var call = Sign(HttpMethod.Get, "/p", null, key: _partnerB);
        Assert.Equal("key_client_mismatch", (await Verify(call.ToView())).Reason);
    }

    [Fact]
    public async Task Given_以另一把私鑰簽署但宣稱partner_a的keyid_When_驗證_Then_簽章無效()
    {
        var call = Sign(HttpMethod.Get, "/p", null, new SignOptions { Now = Now, KeyId = "partner-a-sig-1" }, _partnerB);
        Assert.Equal("signature_invalid", (await Verify(call.ToView())).Reason);
    }

    [Theory]
    [InlineData("rsa-pss-sha512")]
    [InlineData("ed25519")]
    [InlineData("ECDSA-P256-SHA256")]
    [InlineData("hmac-sha256")]
    public async Task Given_alg不是固定演算法_When_驗證_Then_拒絕且不降級(string algorithm)
    {
        var call = Sign(HttpMethod.Get, "/p", null, new SignOptions { Now = Now, Algorithm = algorithm });
        Assert.Equal("algorithm_unsupported", (await Verify(call.ToView())).Reason);
    }

    [Theory]
    [InlineData("created,nonce,keyid,alg")]
    [InlineData("created,expires,keyid,alg")]
    [InlineData("created,expires,nonce,keyid")]
    [InlineData("expires,created,nonce,keyid,alg")]
    [InlineData("created,expires,keyid,nonce,alg")]
    [InlineData("created,expires,nonce,keyid,alg,tag")]
    public async Task Given_簽章參數缺漏_順序錯誤或多出_When_驗證_Then_拒絕(string order)
    {
        var call = Sign(HttpMethod.Get, "/p", null, new SignOptions { Now = Now, ParameterOrder = order.Split(',') });
        Assert.Equal("signature_parameters_invalid", (await Verify(call.ToView())).Reason);
    }

    [Fact]
    public async Task Given_簽章標籤不是sig1_When_驗證_Then_拒絕()
    {
        var call = Sign(HttpMethod.Get, "/p", null, new SignOptions { Now = Now, Label = "sig2" });
        Assert.Equal("signature_label_unsupported", (await Verify(call.ToView())).Reason);
    }

    [Fact]
    public async Task Given_Signature_Input有多個成員_When_驗證_Then_拒絕()
    {
        var call = Sign(HttpMethod.Get, "/p", null);
        var view = call.ToView() with { SignatureInput = [call.SignatureInput + ", " + call.SignatureInput!.Replace("sig1", "sig2")] };
        Assert.Equal("signature_input_malformed", (await Verify(view)).Reason);
    }

    [Theory]
    [InlineData("Signature-Input")]
    [InlineData("Signature")]
    public async Task Given_簽章標頭重複出現多行_When_驗證_Then_拒絕為有歧義(string header)
    {
        var view = Sign(HttpMethod.Get, "/p", null).ToView();
        view = header == "Signature"
            ? view with { Signature = [.. view.Signature, .. view.Signature] }
            : view with { SignatureInput = [.. view.SignatureInput, .. view.SignatureInput] };
        Assert.Equal("signature_ambiguous", (await Verify(view)).Reason);
    }

    [Theory]
    [InlineData("authorization")]
    [InlineData("idempotency-key")]
    [InlineData("content-type")]
    [InlineData("content-digest")]
    public async Task Given_被簽入的標頭重複出現多行_When_驗證_Then_拒絕為有歧義(string header)
    {
        var view = Sign(HttpMethod.Post, "/p", Json).ToView();
        view = header switch
        {
            "authorization" => view with { Authorization = [.. view.Authorization, "Bearer other"] },
            "idempotency-key" => view with { IdempotencyKey = [.. view.IdempotencyKey, "second"] },
            "content-type" => view with { ContentType = [.. view.ContentType, "text/plain"] },
            _ => view with { ContentDigest = [.. view.ContentDigest, .. view.ContentDigest] }
        };
        Assert.Equal("header_ambiguous", (await Verify(view)).Reason);
    }

    [Theory]
    [InlineData("authorization")]
    [InlineData("idempotency-key")]
    [InlineData("content-type")]
    public async Task Given_被簽入的標頭實際缺少_When_驗證_Then_拒絕(string header)
    {
        var view = Sign(HttpMethod.Post, "/p", Json).ToView();
        view = header switch
        {
            "authorization" => view with { Authorization = [] },
            "idempotency-key" => view with { IdempotencyKey = [] },
            _ => view with { ContentType = [] }
        };
        Assert.Equal("header_missing", (await Verify(view)).Reason);
    }

    [Fact]
    public async Task Given_Body簽後被改動_When_驗證_Then_Content_Digest不符()
    {
        var view = Sign(HttpMethod.Post, "/p", Json).ToView() with { Body = Encoding.UTF8.GetBytes("""{"partnerName":"Evil"}""") };
        Assert.Equal("content_digest_mismatch", (await Verify(view)).Reason);
    }

    [Fact]
    public async Task Given_Content_Digest只有sha_512_When_驗證_Then_拒絕()
    {
        var digest = "sha-512=:" + Convert.ToBase64String(SHA512.HashData(Json)) + ":";
        var view = Sign(HttpMethod.Post, "/p", Json).ToView() with { ContentDigest = [digest] };
        Assert.Equal("content_digest_invalid", (await Verify(view)).Reason);
    }

    [Fact]
    public async Task Given_任一被簽入的元件在傳輸後被改動_When_驗證_Then_簽章無效()
    {
        var view = Sign(HttpMethod.Post, "/partner/x?a=1", Json).ToView();
        Assert.True((await Verify(view)).Succeeded);

        Assert.Equal("signature_invalid", (await Verify(view with { Query = "?a=2" })).Reason);
        Assert.Equal("signature_invalid", (await Verify(view with { Path = "/partner/y" })).Reason);
        Assert.Equal("signature_invalid", (await Verify(view with { Method = "PUT" })).Reason);
        Assert.Equal("signature_invalid", (await Verify(view with { Authority = "evil.example:8443" })).Reason);
        Assert.Equal("signature_invalid", (await Verify(view with { Authorization = ["Bearer other-token"] })).Reason);
        Assert.Equal("signature_invalid", (await Verify(view with { IdempotencyKey = ["another"] })).Reason);
        Assert.Equal("signature_invalid", (await Verify(view with { ContentType = ["text/plain"] })).Reason);
    }

    [Fact]
    public async Task Given_Authorization含非OWS空白字元_When_驗證_Then_逐字簽入而不被修剪()
    {
        var nbsp = RequestSigner.Sign(HttpMethod.Post, Authority, "/p", "token-value\u00A0", Json, _partnerA, new SignOptions { Now = Now }).ToView();
        Assert.True((await Verify(nbsp)).Succeeded);

        var plain = Sign(HttpMethod.Post, "/p", Json).ToView();
        Assert.Equal("signature_invalid", (await Verify(plain with { Authorization = [plain.Authorization[0] + "\u00A0"] })).Reason);
        Assert.True((await Verify(plain with { Authorization = ["  " + plain.Authorization[0] + "\t"] })).Succeeded);
    }
}
