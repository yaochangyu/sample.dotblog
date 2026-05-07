using System.Collections.Concurrent;

namespace AuthServer.Services;

public class AccessTokenStore
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(5);
    private readonly ConcurrentDictionary<string, AccessTokenEntry> _store = new();

    public int ExpiresInSeconds => (int)TokenLifetime.TotalSeconds;

    public void Save(string token, string username)
        => _store[token] = new AccessTokenEntry(username, DateTime.UtcNow.Add(TokenLifetime));

    public string? GetUsername(string token)
    {
        if (!_store.TryGetValue(token, out var entry))
            return null;

        if (entry.IsExpired)
        {
            _store.TryRemove(token, out _);
            return null;
        }

        return entry.Username;
    }

    public void Remove(string token) => _store.TryRemove(token, out _);

    private sealed record AccessTokenEntry(string Username, DateTime ExpiresAt)
    {
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }
}
