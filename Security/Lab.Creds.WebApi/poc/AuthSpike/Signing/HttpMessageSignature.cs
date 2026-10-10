using System.Security.Cryptography;
using System.Text;

namespace AuthSpike.Signing;

/// <summary>
/// 呼叫端與業務 API 共用的 HTTP Message Signatures（RFC 9421）規則。
/// 演算法、必要元件與 Content-Digest（RFC 9530）為 lab 提案，待使用者確認（見 03 單）。
/// </summary>
public static class HttpMessageSignature
{
    public const string Label = "sig1";
    public const string Algorithm = "ecdsa-p256-sha256";
    public const string DigestAlgorithm = "sha-256";

    /// <summary>有 Body 的請求（POST）必須涵蓋的元件；涵蓋方法、完整目標、查詢參數、Token 綁定、內容摘要／型別與 Idempotency Key。</summary>
    public static readonly string[] ComponentsWithBody =
        ["@method", "@target-uri", "@query", "authorization", "content-type", "content-digest", "idempotency-key"];

    /// <summary>無 Body 的請求（GET）必須涵蓋的元件。</summary>
    public static readonly string[] ComponentsWithoutBody =
        ["@method", "@target-uri", "@query", "authorization"];

    public static string[] RequiredComponents(string method)
        => string.Equals(method, HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase) ? ComponentsWithBody : ComponentsWithoutBody;

    public static string ContentDigest(ReadOnlySpan<byte> body)
        => $"{DigestAlgorithm}=:{Convert.ToBase64String(SHA256.HashData(body))}:";

    /// <summary>簽章參數（Signature-Input 去掉 label 後的部分），即 RFC 9421 的 @signature-params 值。</summary>
    public static string SignatureParams(IReadOnlyList<string> components, string keyId, DateTimeOffset created, DateTimeOffset expires, string nonce)
        => $"({string.Join(' ', components.Select(component => $"\"{component}\""))})"
           + $";created={created.ToUnixTimeSeconds()};expires={expires.ToUnixTimeSeconds()}"
           + $";keyid=\"{keyId}\";alg=\"{Algorithm}\";nonce=\"{nonce}\"";

    /// <summary>簽章基底（RFC 9421 §2.5）。values 需涵蓋 components 的每一個元件。</summary>
    public static string BuildSignatureBase(IReadOnlyList<string> components, IReadOnlyDictionary<string, string> values, string signatureParams)
    {
        var builder = new StringBuilder();
        foreach (var component in components)
        {
            builder.Append('"').Append(component).Append("\": ").Append(values[component]).Append('\n');
        }

        builder.Append("\"@signature-params\": ").Append(signatureParams);
        return builder.ToString();
    }
}
