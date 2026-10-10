using System.Security.Cryptography;
using System.Text;

namespace AuthSpike.Signing;

/// <summary>簽章金鑰：KeyId 登錄於業務 API，Key 為 ECDSA P-256（與 mTLS 憑證金鑰分開）。</summary>
public sealed record SignatureKey(string KeyId, ECDsa Key);

/// <summary>呼叫服務端：以 Client 的簽章私鑰簽署業務請求（Signature-Input、Signature 與 Content-Digest 標頭）。</summary>
public static class BusinessRequestSigner
{
    public static async Task SignAsync(
        HttpRequestMessage request,
        SignatureKey signatureKey,
        DateTimeOffset created,
        DateTimeOffset expires,
        string nonce)
    {
        var components = HttpMessageSignature.RequiredComponents(request.Method.Method, request.RequestUri!.AbsolutePath);
        var values = new Dictionary<string, string>
        {
            ["@method"] = request.Method.Method,
            ["@target-uri"] = request.RequestUri!.AbsoluteUri,
            ["@query"] = string.IsNullOrEmpty(request.RequestUri.Query) ? "?" : request.RequestUri.Query,
            ["authorization"] = request.Headers.Authorization?.ToString() ?? string.Empty,
        };

        if (components.Contains("content-digest"))
        {
            var body = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync();
            var digest = HttpMessageSignature.ContentDigest(body);
            request.Content!.Headers.Remove("Content-Digest");
            request.Content.Headers.TryAddWithoutValidation("Content-Digest", digest);

            values["content-type"] = request.Content.Headers.GetValues("Content-Type").Single();
            values["content-digest"] = digest;
            if (components.Contains("idempotency-key"))
            {
                values["idempotency-key"] = request.Headers.GetValues("Idempotency-Key").Single();
            }
        }

        var parameters = HttpMessageSignature.SignatureParams(components, signatureKey.KeyId, created, expires, nonce);
        var signatureBase = HttpMessageSignature.BuildSignatureBase(components, values, parameters);
        var signature = signatureKey.Key.SignData(
            Encoding.UTF8.GetBytes(signatureBase),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        request.Headers.TryAddWithoutValidation("Signature-Input", $"{HttpMessageSignature.Label}={parameters}");
        request.Headers.TryAddWithoutValidation("Signature", $"{HttpMessageSignature.Label}=:{Convert.ToBase64String(signature)}:");
    }
}
