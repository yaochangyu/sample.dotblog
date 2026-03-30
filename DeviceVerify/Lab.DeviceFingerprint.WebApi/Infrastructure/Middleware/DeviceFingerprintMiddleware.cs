using System.Security.Cryptography;
using System.Text;

namespace Lab.DeviceFingerprint.WebApi.Infrastructure.Middleware;

public class DeviceFingerprintMiddleware(RequestDelegate next)
{
    private const string FingerprintHeader = "X-Device-Fingerprint";
    private const string FingerprintHashClaim = "fingerprintHash";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var fingerprintClaim = context.User.FindFirst(FingerprintHashClaim)?.Value;

            if (fingerprintClaim is not null)
            {
                if (!context.Request.Headers.TryGetValue(FingerprintHeader, out var fingerprintValue)
                    || string.IsNullOrWhiteSpace(fingerprintValue))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { error = "缺少裝置指紋標頭" });
                    return;
                }

                var incomingHash = HashFingerprint(fingerprintValue.ToString());
                if (!string.Equals(incomingHash, fingerprintClaim, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { error = "裝置指紋不符" });
                    return;
                }
            }
        }

        await next(context);
    }

    private static string HashFingerprint(string fingerprint)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
