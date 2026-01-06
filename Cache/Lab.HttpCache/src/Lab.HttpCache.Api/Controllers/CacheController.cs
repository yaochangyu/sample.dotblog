using Microsoft.AspNetCore.Mvc;
using Lab.HttpCache.Api.Services;

namespace Lab.HttpCache.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
{
    private readonly IMemoryCacheService _memoryCacheService;
    private readonly IRedisCacheService _redisCacheService;
    private readonly ITwoLevelCacheService _twoLevelCacheService;

    public CacheController(
        IMemoryCacheService memoryCacheService,
        IRedisCacheService redisCacheService,
        ITwoLevelCacheService twoLevelCacheService)
    {
        _memoryCacheService = memoryCacheService;
        _redisCacheService = redisCacheService;
        _twoLevelCacheService = twoLevelCacheService;
    }

    /// <summary>
    /// 展示 Memory Cache 的使用
    /// </summary>
    [HttpGet("memory")]
    public IActionResult GetMemoryCache([FromQuery] string key = "memory-test")
    {
        var cachedValue = _memoryCacheService.Get<string>(key);

        if (cachedValue != null)
        {
            return Ok(new
            {
                source = "Memory Cache",
                key,
                value = cachedValue,
                timestamp = DateTime.UtcNow
            });
        }

        var newValue = $"Data generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        _memoryCacheService.Set(key, newValue, TimeSpan.FromMinutes(5));

        return Ok(new
        {
            source = "New Data (cached in Memory)",
            key,
            value = newValue,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 展示 Redis Cache 的使用
    /// </summary>
    [HttpGet("redis")]
    public async Task<IActionResult> GetRedisCache([FromQuery] string key = "redis-test")
    {
        var cachedValue = await _redisCacheService.GetAsync<string>(key);

        if (cachedValue != null)
        {
            return Ok(new
            {
                source = "Redis Cache",
                key,
                value = cachedValue,
                timestamp = DateTime.UtcNow
            });
        }

        var newValue = $"Data generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        await _redisCacheService.SetAsync(key, newValue, TimeSpan.FromMinutes(10));

        return Ok(new
        {
            source = "New Data (cached in Redis)",
            key,
            value = newValue,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 展示二級快取的使用
    /// </summary>
    [HttpGet("two-level")]
    public async Task<IActionResult> GetTwoLevelCache([FromQuery] string key = "two-level-test")
    {
        var cachedValue = await _twoLevelCacheService.GetAsync<string>(key);

        if (cachedValue != null)
        {
            return Ok(new
            {
                source = "Two-Level Cache (Memory or Redis)",
                key,
                value = cachedValue,
                timestamp = DateTime.UtcNow
            });
        }

        var newValue = $"Data generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        await _twoLevelCacheService.SetAsync(key, newValue, TimeSpan.FromMinutes(10));

        return Ok(new
        {
            source = "New Data (cached in both Memory and Redis)",
            key,
            value = newValue,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 展示 HTTP Cache 的使用 - ResponseCache Attribute
    /// </summary>
    [HttpGet("http-response-cache")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public IActionResult GetHttpResponseCache()
    {
        return Ok(new
        {
            source = "HTTP Response Cache (60 seconds)",
            value = $"Data generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 展示 HTTP Cache 的使用 - 手動設定 Cache-Control 標頭
    /// </summary>
    [HttpGet("http-cache-control")]
    public IActionResult GetHttpCacheControl()
    {
        Response.Headers.CacheControl = "public, max-age=120";
        Response.Headers.Expires = DateTime.UtcNow.AddMinutes(2).ToString("R");

        return Ok(new
        {
            source = "HTTP Cache-Control (120 seconds)",
            value = $"Data generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
            timestamp = DateTime.UtcNow,
            headers = new
            {
                CacheControl = "public, max-age=120",
                Expires = Response.Headers.Expires.ToString()
            }
        });
    }

    /// <summary>
    /// 展示 HTTP Cache 的使用 - ETag
    /// </summary>
    [HttpGet("http-etag")]
    public IActionResult GetHttpETag()
    {
        var data = $"Data generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        var etag = $"\"{data.GetHashCode()}\"";

        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "public, max-age=60";

        if (Request.Headers.IfNoneMatch == etag)
        {
            return StatusCode(304); // Not Modified
        }

        return Ok(new
        {
            source = "HTTP ETag",
            value = data,
            timestamp = DateTime.UtcNow,
            etag
        });
    }

    /// <summary>
    /// 清除指定的快取
    /// </summary>
    [HttpDelete("{cacheType}/{key}")]
    public async Task<IActionResult> DeleteCache(string cacheType, string key)
    {
        switch (cacheType.ToLower())
        {
            case "memory":
                _memoryCacheService.Remove(key);
                break;
            case "redis":
                await _redisCacheService.RemoveAsync(key);
                break;
            case "two-level":
                await _twoLevelCacheService.RemoveAsync(key);
                break;
            default:
                return BadRequest($"Unknown cache type: {cacheType}");
        }

        return Ok(new
        {
            message = $"Cache cleared for key: {key} in {cacheType} cache"
        });
    }
}
