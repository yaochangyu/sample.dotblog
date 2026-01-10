using Microsoft.Extensions.Caching.Memory;

namespace Lab.CSRF.WebApi.Services;

public interface ITokenNonceService
{
    string GenerateNonce();
    bool ValidateAndConsumeNonce(string nonce);
}

public class TokenNonceService : ITokenNonceService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _expirationTime = TimeSpan.FromMinutes(30);

    public TokenNonceService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string GenerateNonce()
    {
        var nonce = Guid.NewGuid().ToString("N");
        _cache.Set($"nonce:{nonce}", true, _expirationTime);
        return nonce;
    }

    public bool ValidateAndConsumeNonce(string nonce)
    {
        if (string.IsNullOrEmpty(nonce))
            return false;

        var key = $"nonce:{nonce}";
        if (_cache.TryGetValue(key, out _))
        {
            _cache.Remove(key);
            return true;
        }
        return false;
    }
}
