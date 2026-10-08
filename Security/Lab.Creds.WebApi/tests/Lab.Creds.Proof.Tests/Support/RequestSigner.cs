using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Lab.Creds.Proof.Signatures;
using Microsoft.IdentityModel.Tokens;

namespace Lab.Creds.Proof.Tests.Support;

// A runtime-generated NIST P-256 request-signing key pair; never persisted, independent of the mTLS RSA keys.
public sealed class TestSigningKey(string keyId) : IDisposable
{
    public string KeyId { get; } = keyId;
    public ECDsa Key { get; } = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    public byte[] PublicKey => Key.ExportSubjectPublicKeyInfo();
    public void Dispose() => Key.Dispose();
}

// Overrides used to build each deliberately invalid variant; the defaults produce a profile-conformant signature.
public sealed class SignOptions
{
    public DateTimeOffset? Now { get; init; }
    public long? Created { get; init; }
    public long? Expires { get; init; }
    public string? Nonce { get; init; }
    public string? KeyId { get; init; }
    public string Algorithm { get; init; } = LabSignatureProfile.Algorithm;
    public string Label { get; init; } = LabSignatureProfile.Label;
    public string[]? Components { get; init; }
    public string[]? ParameterOrder { get; init; }
    public string? Authority { get; init; }
}

// The request exactly as it is put on the wire. Tests mutate it after signing to produce tampered requests.
public sealed class SignedCall
{
    public required HttpMethod Method { get; set; }
    public required string Authority { get; set; }
    public required string PathAndQuery { get; set; }
    public byte[]? Body { get; set; }
    public bool Chunked { get; set; }
    public string? ContentType { get; set; } = "application/json";
    public string? Authorization { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? ContentDigest { get; set; }
    public string? SignatureInput { get; set; }
    public string? Signature { get; set; }
    public List<KeyValuePair<string, string>> ExtraHeaders { get; } = [];

    public HttpRequestMessage Build()
    {
        var request = new HttpRequestMessage(Method, new Uri(PathAndQuery, UriKind.Relative));
        if (Body is not null && (Body.Length > 0 || Chunked))
        {
            request.Content = Chunked ? new ChunkedContent(Body) : new ByteArrayContent(Body);
            if (ContentType is not null) request.Content.Headers.TryAddWithoutValidation("Content-Type", ContentType);
        }

        if (Chunked) request.Headers.TransferEncodingChunked = true;
        if (Authorization is not null) request.Headers.TryAddWithoutValidation("Authorization", Authorization);
        if (IdempotencyKey is not null) request.Headers.TryAddWithoutValidation("Idempotency-Key", IdempotencyKey);
        if (ContentDigest is not null) request.Headers.TryAddWithoutValidation("Content-Digest", ContentDigest);
        if (SignatureInput is not null) request.Headers.TryAddWithoutValidation("Signature-Input", SignatureInput);
        if (Signature is not null) request.Headers.TryAddWithoutValidation("Signature", Signature);
        foreach (var (name, value) in ExtraHeaders) request.Headers.TryAddWithoutValidation(name, value);
        return request;
    }

    public SignatureRequestView ToView()
    {
        var split = PathAndQuery.IndexOf('?');
        var path = split < 0 ? PathAndQuery : PathAndQuery[..split];
        var query = split < 0 || split == PathAndQuery.Length - 1 ? "?" : PathAndQuery[split..];
        static string[] One(string? value) => value is null ? [] : [value];
        return new SignatureRequestView(
            Method.Method, Authority.ToLowerInvariant(), path, query,
            One(SignatureInput), One(Signature), One(Authorization), One(Body is { Length: > 0 } ? ContentType : null),
            One(ContentDigest), One(IdempotencyKey), Body ?? []);
    }

    private sealed class ChunkedContent(byte[] bytes) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => stream.WriteAsync(bytes).AsTask();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}

// Independent signer used by the tests. It is written from the profile text, not from the verifier code,
// so that the verifier agreeing with it is evidence of conformance rather than self-consistency.
public static class RequestSigner
{
    private static readonly string[] ReadComponents = ["@method", "@authority", "@path", "@query", "authorization"];
    private static readonly string[] SideEffectComponents = [.. ReadComponents, "idempotency-key"];
    private static readonly string[] BodyComponents = ["@method", "@authority", "@path", "@query", "authorization", "content-type", "content-digest", "idempotency-key"];

    public static SignedCall Sign(
        HttpMethod method, string authority, string pathAndQuery, string? accessToken, byte[]? body,
        TestSigningKey key, SignOptions? options = null)
    {
        options ??= new SignOptions();
        var readOnly = method == HttpMethod.Get || method == HttpMethod.Head;
        var call = new SignedCall
        {
            Method = method,
            Authority = authority,
            PathAndQuery = pathAndQuery,
            Body = body,
            Authorization = accessToken is null ? null : "Bearer " + accessToken,
            IdempotencyKey = readOnly ? null : Guid.NewGuid().ToString("N")
        };
        var hasBody = body is { Length: > 0 };
        if (hasBody) call.ContentDigest = "sha-256=:" + Convert.ToBase64String(SHA256.HashData(body!)) + ":";
        else call.ContentType = null;

        var components = options.Components ?? (readOnly ? ReadComponents : hasBody ? BodyComponents : SideEffectComponents);
        var now = options.Now ?? DateTimeOffset.UtcNow;
        var created = options.Created ?? now.ToUnixTimeSeconds();
        var parameters = new Dictionary<string, string>
        {
            ["created"] = created.ToString(),
            ["expires"] = (options.Expires ?? created + 60).ToString(),
            ["nonce"] = Quote(options.Nonce ?? Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(16))),
            ["keyid"] = Quote(options.KeyId ?? key.KeyId),
            ["alg"] = Quote(options.Algorithm),
            ["tag"] = Quote("extra")
        };
        var order = options.ParameterOrder ?? ["created", "expires", "nonce", "keyid", "alg"];
        var paramsValue = "(" + string.Join(' ', components.Select(Quote)) + ")" +
            string.Concat(order.Select(name => $";{name}={parameters[name]}"));

        var signatureBase = new StringBuilder();
        foreach (var component in components)
        {
            signatureBase.Append(Quote(component)).Append(": ").Append(ComponentValue(call, component, options.Authority)).Append('\n');
        }

        signatureBase.Append("\"@signature-params\": ").Append(paramsValue);
        var signature = key.Key.SignData(Encoding.ASCII.GetBytes(signatureBase.ToString()), HashAlgorithmName.SHA256);
        call.SignatureInput = $"{options.Label}={paramsValue}";
        call.Signature = $"{options.Label}=:{Convert.ToBase64String(signature)}:";
        return call;
    }

    public static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    private static string ComponentValue(SignedCall call, string component, string? authorityOverride)
    {
        var split = call.PathAndQuery.IndexOf('?');
        return component switch
        {
            "@method" => call.Method.Method,
            "@authority" => (authorityOverride ?? call.Authority).ToLowerInvariant(),
            "@scheme" => "https",
            "@path" => split < 0 ? call.PathAndQuery : call.PathAndQuery[..split],
            "@query" => split < 0 || split == call.PathAndQuery.Length - 1 ? "?" : call.PathAndQuery[split..],
            "authorization" => call.Authorization!,
            "content-type" => call.ContentType!,
            "content-digest" => call.ContentDigest!,
            "idempotency-key" => call.IdempotencyKey!,
            _ => throw new ArgumentException(component)
        };
    }
}
