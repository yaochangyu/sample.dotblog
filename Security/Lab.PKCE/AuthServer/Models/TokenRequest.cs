namespace AuthServer.Models;

public class TokenRequest
{
    public string GrantType { get; init; } = "";
    public string Code { get; init; } = "";
    public string CodeVerifier { get; init; } = "";
    public string ClientId { get; init; } = "";
    public string RedirectUri { get; init; } = "";
}
