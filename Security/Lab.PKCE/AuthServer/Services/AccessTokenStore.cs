using System.Collections.Concurrent;

namespace AuthServer.Services;

public class AccessTokenStore
{
    private readonly ConcurrentDictionary<string, string> _store = new();

    public void Save(string token, string username) => _store[token] = username;

    public string? GetUsername(string token)
    {
        _store.TryGetValue(token, out var username);
        return username;
    }

    public void Remove(string token) => _store.TryRemove(token, out _);
}
