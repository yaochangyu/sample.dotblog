using Lab.HttpCache.Api.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;

namespace Lab.HttpCache.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
{
    private readonly ICacheProvider _cacheProvider;
    private readonly HybridCache _hybridCache;

    public CacheController(ICacheProvider cacheProvider, HybridCache hybridCache)
    {
        _cacheProvider = cacheProvider;
        _hybridCache = hybridCache;
    }

    /// <summary>
    /// 展示 HybridCache 基本使用 - 使用封裝的服務
    /// </summary>
    [HttpGet("hybrid")]
    public async Task<IActionResult> GetHybridCache([FromQuery] string key = "hybrid-test")
    {
        var value = await _cacheProvider.GetOrCreateAsync(
            key,
            async cancellationToken =>
            {
                await Task.Delay(100, cancellationToken); // 模擬資料庫查詢
                return $"Data generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            },
            TimeSpan.FromMinutes(5),
            tags: ["demo", "hybrid-cache"]);

        return Ok(new
        {
            source = "HybridCache (L1: Memory + L2: Redis)",
            key,
            value,
            tags = new[] { "demo", "hybrid-cache" },
            timestamp = DateTime.UtcNow,
            description = "HybridCache 自動處理兩級快取，優先從 L1 (記憶體) 讀取，未命中時從 L2 (Redis) 讀取。使用 tags 標籤便於批量清除快取"
        });
    }

    /// <summary>
    /// 展示 HybridCache 直接使用 - 使用 GetOrCreateAsync
    /// </summary>
    [HttpGet("hybrid-direct")]
    public async Task<IActionResult> GetHybridCacheDirect([FromQuery] string key = "hybrid-direct-test")
    {
        var value = await _hybridCache.GetOrCreateAsync(
            key,
            async cancellationToken =>
            {
                await Task.Delay(100, cancellationToken); // 模擬資料庫查詢
                return $"Data generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10),
                LocalCacheExpiration = TimeSpan.FromMinutes(2) // L1 快取 2 分鐘，L2 快取 10 分鐘
            },
            tags: ["demo", "direct-access"]);

        return Ok(new
        {
            source = "HybridCache Direct (L1: 2min, L2: 10min)",
            key,
            value,
            tags = new[] { "demo", "direct-access" },
            timestamp = DateTime.UtcNow,
            description = "L1 (記憶體) 快取 2 分鐘，L2 (Redis) 快取 10 分鐘"
        });
    }

    /// <summary>
    /// 展示 HybridCache 的序列化 - 複雜物件
    /// </summary>
    [HttpGet("hybrid-complex")]
    public async Task<IActionResult> GetHybridCacheComplex([FromQuery] string userId = "user-123")
    {
        var key = $"user:{userId}";

        var userData = await _hybridCache.GetOrCreateAsync(
            key,
            async cancellationToken =>
            {
                await Task.Delay(100, cancellationToken); // 模擬資料庫查詢
                return new UserData
                {
                    Id = userId,
                    Name = "測試使用者",
                    Email = "test@example.com",
                    CreatedAt = DateTime.UtcNow
                };
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(15)
            },
            tags: ["user-data", $"user:{userId}"]);

        return Ok(new
        {
            source = "HybridCache Complex Object",
            key,
            value = userData,
            tags = new[] { "user-data", $"user:{userId}" },
            timestamp = DateTime.UtcNow,
            description = "HybridCache 自動處理複雜物件的序列化/反序列化。可透過 tag 批量清除所有使用者資料或特定使用者資料"
        });
    }

    /// <summary>
    /// 展示 HybridCache 的手動設定
    /// </summary>
    [HttpPost("hybrid-set")]
    public async Task<IActionResult> SetHybridCache(
        [FromQuery] string key,
        [FromQuery] string? tag,
        [FromBody] string value)
    {
        string[]? tags = !string.IsNullOrEmpty(tag) ? [tag] : null;
        await _cacheProvider.SetAsync(key, value, TimeSpan.FromMinutes(10), tags);

        return Ok(new
        {
            message = "Cache set successfully",
            key,
            value,
            tags,
            expiration = "10 minutes"
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
            timestamp = DateTime.UtcNow,
            description = "使用 ResponseCache Attribute 設定客戶端快取"
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
            },
            description = "手動設定 Cache-Control 和 Expires 標頭"
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
            etag,
            description = "使用 ETag 進行條件請求，相同內容回傳 304 Not Modified"
        });
    }

    /// <summary>
    /// 清除指定的快取
    /// </summary>
    [HttpDelete("hybrid/{key}")]
    public async Task<IActionResult> DeleteHybridCache(string key)
    {
        await _cacheProvider.RemoveAsync(key);

        return Ok(new
        {
            message = $"Cache cleared for key: {key}",
            description = "HybridCache 會同時清除 L1 (記憶體) 和 L2 (Redis) 的快取"
        });
    }

    /// <summary>
    /// 透過標籤批量清除快取
    /// </summary>
    [HttpDelete("hybrid/tag/{tag}")]
    public async Task<IActionResult> DeleteHybridCacheByTag(string tag)
    {
        await _cacheProvider.RemoveByTagAsync(tag);

        return Ok(new
        {
            message = $"Cache cleared for tag: {tag}",
            tag,
            description = "HybridCache 會同時清除所有帶有指定標籤的快取項目（L1 和 L2）"
        });
    }

    /// <summary>
    /// 取得快取統計資訊
    /// </summary>
    [HttpGet("stats")]
    public IActionResult GetCacheStats()
    {
        return Ok(new
        {
            hybridCache = new
            {
                description = "HybridCache (.NET 9+)",
                features = new[]
                {
                    "自動兩級快取 (L1: Memory, L2: Distributed)",
                    "Stampede Protection (防止快取穿透)",
                    "自動序列化/反序列化",
                    "支援標籤式快取失效 (Tag-based invalidation)",
                    "更好的效能和記憶體使用"
                }
            },
            httpCache = new
            {
                description = "HTTP Cache (Client-side)",
                features = new[]
                {
                    "ResponseCache Attribute",
                    "Cache-Control 標頭",
                    "ETag 條件請求",
                    "減少伺服器負載"
                }
            }
        });
    }
}

public record UserData
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
