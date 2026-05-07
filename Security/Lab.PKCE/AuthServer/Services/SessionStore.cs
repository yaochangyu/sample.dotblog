using System.Collections.Concurrent;
using AuthServer.Models;

namespace AuthServer.Services;

public class SessionStore
{
    private readonly ConcurrentDictionary<string, Session> _store = new();

    public Session Create(string username)
    {
        var session = new Session
        {
            SessionId = GenerateId(),
            Username  = username
        };
        _store[session.SessionId] = session;
        return session;
    }

    public Session? Get(string sessionId)
    {
        _store.TryGetValue(sessionId, out var session);
        return session;
    }

    public void Remove(string sessionId) => _store.TryRemove(sessionId, out _);

    private static string GenerateId()
        => Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
}
