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

    private static bool TryReadCertificate(Microsoft.Extensions.Primitives.StringValues header, out X509Certificate2 certificate)
    {
        certificate = null!;
        if (header.Count != 1) return false;

        // Ambiguity (several XFCC elements or several Cert fields) is rejected rather than "first one wins".
        var matches = CertPattern().Matches(header[0]!);
        if (matches.Count != 1) return false;
        var match = matches[0];
        try
        {
            certificate = X509Certificate2.CreateFromPem(Uri.UnescapeDataString(match.Groups[1].Value));
            return true;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return false;
        }
    }

    [GeneratedRegex("(?:^|[;,]\\s*)Cert=\"([^\"]+)\"")]
    private static partial Regex CertPattern();
}
