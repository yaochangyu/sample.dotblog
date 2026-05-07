namespace AuthServer.Models;

public class AuthorizeRequest
{
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public string ClientId { get; init; } = "";
    public string RedirectUri { get; init; } = "";
    public string CodeChallenge { get; init; } = "";
    public string CodeChallengeMethod { get; init; } = "S256";
}
