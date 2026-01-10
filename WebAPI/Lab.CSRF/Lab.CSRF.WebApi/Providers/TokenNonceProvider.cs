using Microsoft.Extensions.Caching.Distributed;

namespace Lab.CSRF.WebApi.Providers;

public interface ITokenNonceProvider
{
    Task<string> GenerateNonceAsync();
    Task<bool> ValidateAndConsumeNonceAsync(string nonce);
}

public class TokenNonceProvider : ITokenNonceProvider
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _expirationTime = TimeSpan.FromMinutes(30);

    public TokenNonceProvider(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<string> GenerateNonceAsync()
    {
        var nonce = Guid.NewGuid().ToString("N");
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _expirationTime
        };
        await _cache.SetStringAsync($"nonce:{nonce}", "true", options);
        return nonce;
    }

    public async Task<bool> ValidateAndConsumeNonceAsync(string nonce)
    {
        if (string.IsNullOrEmpty(nonce))
            return false;

        var key = $"nonce:{nonce}";
        var value = await _cache.GetStringAsync(key);
        if (value != null)
        {
            await _cache.RemoveAsync(key);
            return true;
        }
        return false;
    }
}
