using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Lab.Creds.Proof;

// Runs only on the Gateway-facing API. The original client certificate is read from the single
// X-Forwarded-Client-Cert header that Envoy overwrites (SANITIZE_SET) and is exposed to the token
// validation as the connection's client certificate. No other forwarded identity header is trusted.
public sealed partial class GatewayClientCertificateMiddleware(RequestDelegate next)
{
    public const string HeaderName = "x-forwarded-client-cert";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!TryReadCertificate(context.Request.Headers[HeaderName], out var certificate))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        using (certificate)
        {
            context.Connection.ClientCertificate = certificate;
            await next(context);
        }
    }

    // Envoy v1.39.3 (SANITIZE_SET + cert:true) emits exactly one element: Hash=<sha256 hex>;Cert="<URL-encoded PEM>".
    // Anything else is rejected: the whole value must be that single element, strictly percent-encoded,
    // decoding to exactly one PEM certificate whose SHA-256 equals the Hash field.
    private static bool TryReadCertificate(Microsoft.Extensions.Primitives.StringValues header, out X509Certificate2 certificate)
    {
        certificate = null!;
        if (header.Count != 1) return false;

        var match = ElementPattern().Match(header[0]!);
        if (!match.Success) return false;

        var pem = Uri.UnescapeDataString(match.Groups[2].Value);
        if (!PemEncoding.TryFind(pem, out var fields) || !pem.AsSpan(fields.Label).SequenceEqual("CERTIFICATE")) return false;
        if (pem.AsSpan(0, fields.Location.Start.Value).Trim().Length != 0 || pem.AsSpan(fields.Location.End.Value).Trim().Length != 0) return false;
        try
        {
            certificate = X509Certificate2.CreateFromPem(pem);
            if (Convert.ToHexStringLower(SHA256.HashData(certificate.RawData)) == match.Groups[1].Value) return true;

            certificate.Dispose();
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    [GeneratedRegex("^Hash=([0-9a-f]{64});Cert=\"((?:[A-Za-z0-9\\-._~]|%[0-9A-Fa-f]{2})+)\"\\z")]
    private static partial Regex ElementPattern();
}
