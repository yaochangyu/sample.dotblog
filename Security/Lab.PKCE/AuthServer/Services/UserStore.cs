using AuthServer.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.Services;

public class UserStore
{
    private readonly Dictionary<string, User> _users;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public UserStore()
    {
        _users = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase)
        {
            ["alice"] = CreateUser("alice", "password123"),
            ["bob"]   = CreateUser("bob", "password456"),
        };
    }

    public bool Validate(string username, string password)
    {
        if (!_users.TryGetValue(username, out var user))
            return false;

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    private User CreateUser(string username, string password)
        => new()
        {
            Username = username,
            PasswordHash = _passwordHasher.HashPassword(new User { Username = username }, password)
        };
}
