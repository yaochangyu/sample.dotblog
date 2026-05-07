namespace AuthServer.Models;

public class AuthorizationCode
{
    public string ClientId { get; init; } = "";
    public string RedirectUri { get; init; } = "";
    public string CodeChallenge { get; init; } = "";
    public string CodeChallengeMethod { get; init; } = "S256";
    public string Username { get; init; } = "";
    public DateTime ExpiresAt { get; init; } = DateTime.UtcNow.AddMinutes(5);

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
