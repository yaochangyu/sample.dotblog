using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Lab.Creds.Proof.Signatures;

// RFC 9421 Lab Profile v1 (spec.md 〈請求簽章與防重放〉). Pure and side-effect free except for the key lookup callback.
// Ticket 03 limit: nonce is validated for format only. It is NOT recorded or de-duplicated, so a valid signature can be
// resent inside its acceptance window ([created-30, created+90]); replay protection is Ticket 04.
public static class LabSignatureProfile
{
    public const string Label = "sig1";
    public const string Algorithm = "ecdsa-p256-sha256";
    public const int MaxLifetimeSeconds = 60;
    public const int ClockSkewSeconds = 30;

    private static readonly string[] ParameterNames = ["created", "expires", "nonce", "keyid", "alg"];
    private static readonly string[] ReadComponents = ["@method", "@authority", "@path", "@query", "authorization"];
    private static readonly string[] SideEffectComponents = [.. ReadComponents, "idempotency-key"];
    private static readonly string[] BodyComponents = [.. ReadComponents, "content-type", "content-digest", "idempotency-key"];

    public static async Task<SignatureVerification> VerifyAsync(
        SignatureRequestView request, string tokenClientId, DateTimeOffset now, SigningKeyLookup lookup, CancellationToken cancellationToken)
    {
        if (request.SignatureInput.Count == 0 || request.Signature.Count == 0) return SignatureVerification.Fail("signature_missing");
        if (request.SignatureInput.Count != 1 || request.Signature.Count != 1) return SignatureVerification.Fail("signature_ambiguous");

        if (!StructuredFieldParser.TryParseSignatureInput(request.SignatureInput[0], out var input)) return SignatureVerification.Fail("signature_input_malformed");
        if (!StructuredFieldParser.TryParseSignature(request.Signature[0], out var signatureLabel, out var signature) || signature.Length != 64)
        {
            return SignatureVerification.Fail("signature_malformed");
        }

        if (input.Label != Label || signatureLabel != Label) return SignatureVerification.Fail("signature_label_unsupported");

        if (!input.Parameters.Select(p => p.Name).SequenceEqual(ParameterNames)
            || input.Parameters[0].Value is not long created
            || input.Parameters[1].Value is not long expires
            || input.Parameters[2].Value is not string nonce
            || input.Parameters[3].Value is not string keyId
            || input.Parameters[4].Value is not string algorithm)
        {
            return SignatureVerification.Fail("signature_parameters_invalid");
        }

        if (algorithm != Algorithm) return SignatureVerification.Fail("algorithm_unsupported");

        var unixNow = now.ToUnixTimeSeconds();
        if (created < 0 || expires <= created || expires - created > MaxLifetimeSeconds) return SignatureVerification.Fail("signature_time_invalid");
        if (unixNow < created - ClockSkewSeconds) return SignatureVerification.Fail("signature_not_yet_valid");
        if (unixNow > expires + ClockSkewSeconds) return SignatureVerification.Fail("signature_expired");
        if (!IsValidNonce(nonce)) return SignatureVerification.Fail("nonce_invalid");

        var key = await lookup(keyId, cancellationToken);
        if (key is null) return SignatureVerification.Fail("key_unknown");
        if (!string.Equals(key.ClientId, tokenClientId, StringComparison.Ordinal)) return SignatureVerification.Fail("key_client_mismatch", key.KeyId);
        if (!key.IsActive) return SignatureVerification.Fail("key_inactive", key.KeyId);
        if (key.Algorithm != Algorithm) return SignatureVerification.Fail("algorithm_unsupported", key.KeyId);

        var hasBody = !request.Body.IsEmpty;
        string[] expected;
        switch (request.Method)
        {
            case "GET" or "HEAD":
                if (hasBody) return SignatureVerification.Fail("body_not_allowed", key.KeyId);
                expected = ReadComponents;
                break;
            case "POST" or "PUT" or "PATCH" or "DELETE":
                expected = hasBody ? BodyComponents : SideEffectComponents;
                break;
            default:
                return SignatureVerification.Fail("method_unsupported", key.KeyId);
        }

        if (!input.Components.SequenceEqual(expected)) return SignatureVerification.Fail("components_mismatch", key.KeyId);

        foreach (var component in expected.Where(c => !c.StartsWith('@')))
        {
            var values = HeaderValues(request, component);
            if (values.Count > 1) return SignatureVerification.Fail("header_ambiguous", key.KeyId);
            if (values.Count == 0 || string.IsNullOrWhiteSpace(values[0])) return SignatureVerification.Fail("header_missing", key.KeyId);
        }

        if (hasBody)
        {
            if (!StructuredFieldParser.TryParseDigestDictionary(request.ContentDigest[0], out var digests) || !digests.TryGetValue("sha-256", out var digest))
            {
                return SignatureVerification.Fail("content_digest_invalid", key.KeyId);
            }

            // The digest is over the received bytes; the body is never re-serialized.
            if (!CryptographicOperations.FixedTimeEquals(digest, SHA256.HashData(request.Body.Span))) return SignatureVerification.Fail("content_digest_mismatch", key.KeyId);
        }

        var signatureBase = HttpMessageSignature.BuildSignatureBase(input, component => ComponentValue(request, component));
        try
        {
            return HttpMessageSignature.VerifyEcdsaP256(key.PublicKey, signatureBase, signature)
                ? new SignatureVerification(true, null, key.KeyId)
                : SignatureVerification.Fail("signature_invalid", key.KeyId);
        }
        catch (CryptographicException)
        {
            return SignatureVerification.Fail("key_invalid", key.KeyId);
        }
    }

    // 16 CSPRNG bytes as unpadded base64url: exactly 22 characters in canonical form.
    public static bool IsValidNonce(string nonce)
    {
        if (nonce.Length != 22 || !nonce.All(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_')) return false;
        try
        {
            var bytes = Base64Url.DecodeFromChars(nonce);
            return bytes.Length == 16 && Base64Url.EncodeToString(bytes) == nonce;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static IReadOnlyList<string> HeaderValues(SignatureRequestView request, string name) => name switch
    {
        "authorization" => request.Authorization,
        "content-type" => request.ContentType,
        "content-digest" => request.ContentDigest,
        "idempotency-key" => request.IdempotencyKey,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
    };

    private static string ComponentValue(SignatureRequestView request, string name) => name switch
    {
        "@method" => request.Method,
        "@authority" => request.Authority,
        "@path" => request.Path,
        "@query" => request.Query,
        // RFC 9421 2.1 strips only HTTP optional whitespace (SP / HTAB); other characters are signed as received.
        _ => HeaderValues(request, name)[0].Trim(' ', '\t')
    };
}
