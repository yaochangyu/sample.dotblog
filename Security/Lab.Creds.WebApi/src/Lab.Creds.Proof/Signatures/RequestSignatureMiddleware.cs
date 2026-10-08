using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Lab.Creds.Proof.Signatures;

// Applies the Lab Profile to every endpoint that survives authentication and authorization (fail-closed: there is
// no per-endpoint opt-out). Runs after authorization so a missing/invalid token or certificate binding keeps its own 401.
public sealed class RequestSignatureMiddleware(
    RequestDelegate next, IDbContextFactory<ProofDbContext> dbFactory, ILogger<RequestSignatureMiddleware> logger)
{
    public const string VerifiedKeyIdItem = "verified-signature-key-id";
    public const int MaxBodyBytes = 1024 * 1024;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.GetEndpoint() is null)
        {
            await next(context);
            return;
        }

        var cancellationToken = context.RequestAborted;
        var clientId = context.User.GetClaim(OpenIddictConstants.Claims.ClientId);
        if (string.IsNullOrEmpty(clientId))
        {
            await Reject(context, "client_unknown", null, null);
            return;
        }

        var body = await ReadBodyAsync(context.Request, cancellationToken);
        if (body is null)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        context.Request.Body = new MemoryStream(body, writable: false);

        SignatureVerification result;
        try
        {
            result = await LabSignatureProfile.VerifyAsync(
                BuildView(context.Request, body), clientId, TimeProvider.System.GetUtcNow(), LookupAsync, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError("Signature key store unavailable: {ExceptionType}", exception.GetType().Name);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        if (!result.Succeeded)
        {
            await Reject(context, result.Reason!, result.KeyId, clientId);
            return;
        }

        context.Items[VerifiedKeyIdItem] = result.KeyId;
        await next(context);
    }

    private async ValueTask<RegisteredSigningKey?> LookupAsync(string keyId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.SigningKeys.AsNoTracking().SingleOrDefaultAsync(key => key.KeyId == keyId, cancellationToken);
    }

    private static async Task<byte[]?> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength > MaxBodyBytes) return null;
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;
        while ((read = await request.Body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > MaxBodyBytes) return null;
            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    // Uses the raw request target, not the decoded Path, because the signature covers the bytes the caller sent.
    private static SignatureRequestView BuildView(HttpRequest request, byte[] body)
    {
        var rawTarget = request.HttpContext.Features.GetRequiredFeature<IHttpRequestFeature>().RawTarget;
        var split = rawTarget.IndexOf('?');
        var path = split < 0 ? rawTarget : rawTarget[..split];
        var query = split < 0 || split == rawTarget.Length - 1 ? "?" : rawTarget[split..];
        string[] Lines(string name) => request.Headers.TryGetValue(name, out var values) ? values.OfType<string>().ToArray() : [];
        return new SignatureRequestView(
            request.Method, request.Host.Value ?? "", path, query,
            Lines("Signature-Input"), Lines("Signature"), Lines("Authorization"), Lines("Content-Type"),
            Lines("Content-Digest"), Lines("Idempotency-Key"), body, request.Scheme);
    }

    // Only the reason code, the Client and a key id that matched a registered key are logged; never the token,
    // Authorization value, signature value or signature base.
    private async Task Reject(HttpContext context, string reason, string? keyId, string? clientId)
    {
        logger.LogWarning("Request signature rejected: reason={Reason} client_id={ClientId} keyid={KeyId}", reason, clientId, keyId);
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = "Bearer";
        await context.Response.WriteAsJsonAsync(new { error = "invalid_request_signature", reason }, context.RequestAborted);
    }
}
