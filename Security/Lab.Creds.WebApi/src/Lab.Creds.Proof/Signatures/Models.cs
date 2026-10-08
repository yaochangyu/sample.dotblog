namespace Lab.Creds.Proof.Signatures;

public sealed record SignatureParameter(string Name, object Value);

// Components are the quoted identifiers of the inner list; SignatureParamsValue is the exact text after "label=",
// which the strict parser guarantees is already the canonical RFC 8941 serialization.
public sealed record ParsedSignatureInput(
    string Label, IReadOnlyList<string> Components, IReadOnlyList<SignatureParameter> Parameters, string SignatureParamsValue);

// Everything the verifier is allowed to look at. Header lists carry every received field line so that
// repeated headers are detected instead of silently merged.
public sealed record SignatureRequestView(
    string Method,
    string Authority, // raw Host as received; normalized only when the @authority component is derived
    string Path,
    string Query,
    IReadOnlyList<string> SignatureInput,
    IReadOnlyList<string> Signature,
    IReadOnlyList<string> Authorization,
    IReadOnlyList<string> ContentType,
    IReadOnlyList<string> ContentDigest,
    IReadOnlyList<string> IdempotencyKey,
    ReadOnlyMemory<byte> Body,
    string Scheme = "https");

public sealed record SignatureVerification(bool Succeeded, string? Reason, string? KeyId = null)
{
    public static SignatureVerification Fail(string reason, string? keyId = null) => new(false, reason, keyId);
}

public delegate ValueTask<RegisteredSigningKey?> SigningKeyLookup(string keyId, CancellationToken cancellationToken);

// Server-side registration: public key only, bound to exactly one Client and one fixed algorithm.
public sealed class RegisteredSigningKey
{
    public required string KeyId { get; init; }
    public required string ClientId { get; init; }
    public required string Algorithm { get; init; }
    public required byte[] PublicKey { get; init; }
    public bool IsActive { get; init; } = true;
}
