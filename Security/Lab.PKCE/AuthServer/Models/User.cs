namespace AuthServer.Models;

public class User
{
    public string Username { get; init; } = "";
    public string PasswordHash { get; init; } = "";
}
