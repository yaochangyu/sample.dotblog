namespace AuthServer.Models;

public class Session
{
    public string SessionId { get; init; } = "";
    public string Username { get; init; } = "";
    public DateTime ExpiresAt { get; init; } = DateTime.UtcNow.AddHours(8);

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
