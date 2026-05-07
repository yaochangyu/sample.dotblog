using System.Security.Cryptography;
using System.Text;
using AuthServer.Models;

namespace AuthServer.Services;

public class UserStore
{
    private readonly Dictionary<string, User> _users;

    public UserStore()
    {
        _users = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase)
        {
            ["alice"] = new User { Username = "alice", PasswordHash = Hash("password123") },
            ["bob"]   = new User { Username = "bob",   PasswordHash = Hash("password456") },
        };
    }

    public bool Validate(string username, string password)
    {
        if (!_users.TryGetValue(username, out var user))
            return false;

        // 固定時間比較，避免 timing attack
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(user.PasswordHash),
            Encoding.UTF8.GetBytes(Hash(password))
        );
    }

    private static string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
