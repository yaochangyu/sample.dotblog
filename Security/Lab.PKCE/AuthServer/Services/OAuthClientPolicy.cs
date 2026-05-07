using Microsoft.AspNetCore.Http;

namespace AuthServer.Services;

public static class OAuthClientPolicy
{
    public const string ClientId = "spa-lab";
    private const string RedirectPath = "/emulator.html";

    public static bool IsValid(string clientId, string redirectUri, HttpRequest request)
        => string.Equals(clientId, ClientId, StringComparison.Ordinal)
           && string.Equals(redirectUri, GetRedirectUri(request), StringComparison.Ordinal);

    public static string GetRedirectUri(HttpRequest request)
        => $"{request.Scheme}://{request.Host}{RedirectPath}";
}
