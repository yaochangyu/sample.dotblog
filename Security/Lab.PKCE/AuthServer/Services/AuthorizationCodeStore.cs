using System.Collections.Concurrent;
using AuthServer.Models;

namespace AuthServer.Services;

public class AuthorizationCodeStore
{
    private readonly ConcurrentDictionary<string, AuthorizationCode> _store = new();

    public string Save(AuthorizationCode code)
    {
        var key = GenerateCode();
        _store[key] = code;
        return key;
    }

    // 取出後立即移除，確保 code 一次性使用
    public AuthorizationCode? TakeAndRemove(string code)
    {
        _store.TryRemove(code, out var entry);
        return entry;
    }

    private static string GenerateCode()
        => $"CODE_{Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", "").Replace("+", "-").Replace("/", "_")}";
}
