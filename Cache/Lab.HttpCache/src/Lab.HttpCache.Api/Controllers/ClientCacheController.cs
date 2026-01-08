using Microsoft.AspNetCore.Mvc;
using Lab.HttpCache.Api.Repositories;

namespace Lab.HttpCache.Api.Controllers;

/// <summary>
/// HTTP Client-Side Cache 實驗控制器
/// 根據 RFC 9111 規範實作各種 Cache-Control 指令的測試端點
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ClientCacheController : ControllerBase
{
    private static readonly DateTime StartTime = DateTime.UtcNow;
    private static int _requestCounter = 0;
    private readonly IArticleRepository _repository;

    public ClientCacheController(IArticleRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 1. max-age - 標準快取，指定秒數內可重複使用
    /// </summary>
    [HttpGet("max-age")]
    public IActionResult GetMaxAge([FromQuery] int seconds = 60)
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        Response.Headers.CacheControl = $"public, max-age={seconds}";

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            value = $"此回應可快取 {seconds} 秒",
            cacheControl = $"public, max-age={seconds}",
            description = "RFC 9111: max-age 指定回應在多少秒內被視為新鮮。瀏覽器會在此期間內直接使用快取，不會發送請求到伺服器"
        });
    }

    /// <summary>
    /// 2. no-cache - 必須向伺服器驗證後才能使用快取
    /// </summary>
    [HttpGet("no-cache")]
    public IActionResult GetNoCache()
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        var etag = $"\"{requestId}\"";

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.ETag = etag;

        // 檢查 If-None-Match
        if (Request.Headers.IfNoneMatch == etag)
        {
            return StatusCode(304); // Not Modified
        }

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            etag,
            cacheControl = "no-cache",
            description = "RFC 9111: no-cache 表示快取可以儲存回應，但每次使用前必須向伺服器驗證。瀏覽器會發送帶有 If-None-Match 的請求"
        });
    }

    /// <summary>
    /// 3. no-store - 完全禁止快取
    /// </summary>
    [HttpGet("no-store")]
    public IActionResult GetNoStore()
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        Response.Headers.CacheControl = "no-store";

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            cacheControl = "no-store",
            description = "RFC 9111: no-store 指示快取不得儲存此請求或回應的任何部分。每次請求都會完整執行，適用於敏感資料"
        });
    }

    /// <summary>
    /// 4. private - 只能被私有快取（瀏覽器）快取，不能被共享快取（CDN、Proxy）快取
    /// </summary>
    [HttpGet("private")]
    public IActionResult GetPrivate()
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        Response.Headers.CacheControl = "private, max-age=60";

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            cacheControl = "private, max-age=60",
            description = "RFC 9111: private 限制只有私有快取（如使用者的瀏覽器）可以儲存回應，共享快取（如 CDN）不得儲存。適用於使用者特定的資料"
        });
    }

    /// <summary>
    /// 5. public - 明確表示可以被任何快取（包括 CDN）快取
    /// </summary>
    [HttpGet("public")]
    public IActionResult GetPublic()
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        Response.Headers.CacheControl = "public, max-age=3600";

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            cacheControl = "public, max-age=3600",
            description = "RFC 9111: public 明確表示回應可以被任何快取儲存，即使通常不可快取（如需要授權的請求）。適用於公開資源"
        });
    }

    /// <summary>
    /// 6. must-revalidate - 一旦過期，必須向伺服器驗證，不能使用過期的快取
    /// </summary>
    [HttpGet("must-revalidate")]
    public IActionResult GetMustRevalidate()
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        var etag = $"\"{DateTime.UtcNow.Ticks}\"";

        Response.Headers.CacheControl = "max-age=10, must-revalidate";
        Response.Headers.ETag = etag;

        // 檢查條件請求
        if (Request.Headers.IfNoneMatch == etag)
        {
            return StatusCode(304);
        }

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            etag,
            cacheControl = "max-age=10, must-revalidate",
            description = "RFC 9111: must-revalidate 表示快取過期後，不能使用過期的回應，必須向伺服器重新驗證。防止使用過時的資料"
        });
    }

    /// <summary>
    /// 7. immutable - 表示內容永遠不會改變（非 RFC 9111 標準，但被廣泛支援）
    /// </summary>
    [HttpGet("immutable")]
    public IActionResult GetImmutable()
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            cacheControl = "public, max-age=31536000, immutable",
            description = "immutable (RFC 8246): 表示內容在 max-age 期間內永遠不會改變。瀏覽器不會發送條件請求，即使使用者重新整理頁面。適用於 versioned 的靜態資源"
        });
    }

    /// <summary>
    /// 8. Last-Modified 與 If-Modified-Since 驗證
    /// </summary>
    [HttpGet("last-modified")]
    public IActionResult GetLastModified()
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        var lastModified = StartTime;

        Response.Headers.CacheControl = "max-age=30";
        Response.Headers.LastModified = lastModified.ToString("R");

        // 檢查 If-Modified-Since
        if (Request.Headers.IfModifiedSince.FirstOrDefault() is string ifModifiedSinceStr)
        {
            if (DateTime.TryParse(ifModifiedSinceStr, out var ifModifiedSince))
            {
                if (lastModified <= ifModifiedSince)
                {
                    return StatusCode(304);
                }
            }
        }

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            lastModified = lastModified.ToString("R"),
            cacheControl = "max-age=30",
            description = "RFC 9111: Last-Modified 提供資源的最後修改時間。瀏覽器在重新驗證時會發送 If-Modified-Since 標頭進行比較"
        });
    }

    /// <summary>
    /// 9. ETag 強驗證
    /// </summary>
    [HttpGet("etag-strong")]
    public IActionResult GetETagStrong()
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        var contentVersion = "v1.0";
        var etag = $"\"{contentVersion}\""; // 強 ETag（沒有 W/ 前綴）

        Response.Headers.CacheControl = "max-age=30";
        Response.Headers.ETag = etag;

        // 檢查 If-None-Match
        if (Request.Headers.IfNoneMatch.Contains(etag))
        {
            return StatusCode(304);
        }

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            etag,
            contentVersion,
            cacheControl = "max-age=30",
            description = "RFC 9111: 強 ETag（無 W/ 前綴）表示位元組級別的精確匹配。用於 If-None-Match 條件請求"
        });
    }

    /// <summary>
    /// 10. ETag 弱驗證
    /// </summary>
    [HttpGet("etag-weak")]
    public IActionResult GetETagWeak()
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        var semanticVersion = "content-v2";
        var etag = $"W/\"{semanticVersion}\""; // 弱 ETag（W/ 前綴）

        Response.Headers.CacheControl = "max-age=30";
        Response.Headers.ETag = etag;

        // 檢查 If-None-Match（弱比較）
        if (Request.Headers.IfNoneMatch.Any(tag => tag == etag))
        {
            return StatusCode(304);
        }

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            etag,
            semanticVersion,
            cacheControl = "max-age=30",
            description = "RFC 9111: 弱 ETag（W/ 前綴）表示語意等價。內容可能有微小差異（如 gzip），但語意上相同"
        });
    }

    /// <summary>
    /// 11. Vary 標頭 - 根據請求標頭的不同提供不同的快取
    /// </summary>
    [HttpGet("vary")]
    public IActionResult GetVary()
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        var acceptEncoding = Request.Headers.AcceptEncoding.FirstOrDefault() ?? "none";
        var userAgent = Request.Headers.UserAgent.FirstOrDefault() ?? "unknown";

        Response.Headers.CacheControl = "public, max-age=60";
        Response.Headers.Vary = "Accept-Encoding, User-Agent";

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            acceptEncoding,
            userAgent,
            vary = "Accept-Encoding, User-Agent",
            description = "RFC 9111: Vary 指定哪些請求標頭會影響回應內容。快取必須為每個不同的標頭組合儲存不同的版本"
        });
    }

    /// <summary>
    /// 12. s-maxage - 針對共享快取（CDN）的 max-age，會覆蓋 max-age
    /// </summary>
    [HttpGet("s-maxage")]
    public IActionResult GetSMaxage()
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        Response.Headers.CacheControl = "public, max-age=60, s-maxage=3600";

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            cacheControl = "public, max-age=60, s-maxage=3600",
            description = "RFC 9111: s-maxage 覆蓋 max-age，但只針對共享快取（如 CDN）。私有快取（瀏覽器）仍使用 max-age。CDN 快取 1 小時，瀏覽器快取 1 分鐘"
        });
    }

    /// <summary>
    /// 13. stale-while-revalidate - 允許使用過期快取，同時在背景重新驗證
    /// </summary>
    [HttpGet("stale-while-revalidate")]
    public IActionResult GetStaleWhileRevalidate()
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        Response.Headers.CacheControl = "max-age=10, stale-while-revalidate=60";

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            cacheControl = "max-age=10, stale-while-revalidate=60",
            description = "RFC 5861: 回應在 10 秒內新鮮。過期後 60 秒內，可以立即回傳過期的快取給使用者，同時在背景重新驗證。提升使用者體驗"
        });
    }

    /// <summary>
    /// 14. stale-if-error - 當伺服器錯誤時，允許使用過期的快取
    /// </summary>
    [HttpGet("stale-if-error")]
    public IActionResult GetStaleIfError([FromQuery] bool simulateError = false)
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        Response.Headers.CacheControl = "max-age=10, stale-if-error=86400";

        if (simulateError)
        {
            return StatusCode(500, new
            {
                error = "模擬伺服器錯誤",
                description = "當發生此錯誤時，瀏覽器可以使用過期的快取（最多 24 小時）"
            });
        }

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            cacheControl = "max-age=10, stale-if-error=86400",
            description = "RFC 5861: 當伺服器回傳錯誤（5xx）時，瀏覽器可以使用過期的快取，最多 24 小時。提高可用性"
        });
    }

    /// <summary>
    /// 15. max-age=0 - 等同於 no-cache，每次都要重新驗證
    /// </summary>
    [HttpGet("max-age-zero")]
    public IActionResult GetMaxAgeZero()
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        var etag = $"\"{DateTime.UtcNow.Ticks}\"";

        Response.Headers.CacheControl = "max-age=0";
        Response.Headers.ETag = etag;

        if (Request.Headers.IfNoneMatch == etag)
        {
            return StatusCode(304);
        }

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            etag,
            cacheControl = "max-age=0",
            description = "RFC 9111: max-age=0 表示回應立即過期，效果類似 no-cache。每次請求都需要重新驗證"
        });
    }

    /// <summary>
    /// 16. 組合測試：Cache-Control + ETag + Last-Modified
    /// </summary>
    [HttpGet("combined")]
    public IActionResult GetCombined()
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        var lastModified = StartTime;
        var etag = $"\"{lastModified.Ticks}\"";

        Response.Headers.CacheControl = "public, max-age=60, must-revalidate";
        Response.Headers.ETag = etag;
        Response.Headers.LastModified = lastModified.ToString("R");

        // ETag 優先於 Last-Modified
        if (Request.Headers.IfNoneMatch.Contains(etag))
        {
            return StatusCode(304);
        }

        if (Request.Headers.IfModifiedSince.FirstOrDefault() is string ifModifiedSinceStr)
        {
            if (DateTime.TryParse(ifModifiedSinceStr, out var ifModifiedSince))
            {
                if (lastModified <= ifModifiedSince)
                {
                    return StatusCode(304);
                }
            }
        }

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            etag,
            lastModified = lastModified.ToString("R"),
            cacheControl = "public, max-age=60, must-revalidate",
            description = "組合使用 Cache-Control、ETag 和 Last-Modified。ETag 比 Last-Modified 更精確，會優先使用"
        });
    }

    /// <summary>
    /// 取得快取實驗統計資訊
    /// </summary>
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        return Ok(new
        {
            totalRequests = _requestCounter,
            serverStartTime = StartTime,
            uptime = DateTime.UtcNow - StartTime,
            description = "顯示伺服器啟動後收到的總請求數，用於驗證快取是否有效運作"
        });
    }

    /// <summary>
    /// 重置計數器
    /// </summary>
    [HttpPost("reset")]
    public IActionResult ResetStats()
    {
        _requestCounter = 0;
        return Ok(new
        {
            message = "計數器已重置",
            currentCount = _requestCounter
        });
    }

    /// <summary>
    /// 17. 展示客戶端如何透過 Request Cache-Control 指令控制快取行為
    /// </summary>
    [HttpGet("client-controlled")]
    public IActionResult GetClientControlled()
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        var etag = $"\"{DateTime.UtcNow.Ticks}\"";

        // 讀取客戶端發送的 Cache-Control 指令
        var clientCacheControl = Request.Headers.CacheControl.FirstOrDefault() ?? "none";

        // 伺服器端設定：允許快取 60 秒
        Response.Headers.CacheControl = "public, max-age=60";
        Response.Headers.ETag = etag;

        // 處理客戶端的 no-cache 請求
        var clientWantsNoCache = Request.Headers.CacheControl.Any(cc => cc?.Contains("no-cache") == true);

        // 處理客戶端的 max-age 請求
        var clientMaxAge = ExtractClientMaxAge(Request.Headers.CacheControl);

        // 檢查條件請求
        if (Request.Headers.IfNoneMatch == etag)
        {
            return StatusCode(304);
        }

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            etag,
            serverCacheControl = "public, max-age=60",
            clientCacheControl,
            clientWantsNoCache,
            clientMaxAge,
            explanation = new
            {
                concept = "客戶端可以透過請求標頭的 Cache-Control 指令控制快取行為",
                examples = new[]
                {
                    "Cache-Control: no-cache - 強制快取重新驗證",
                    "Cache-Control: no-store - 禁止儲存快取",
                    "Cache-Control: max-age=30 - 只接受 30 秒內的快取",
                    "Cache-Control: only-if-cached - 只使用快取，不發送伺服器請求"
                }
            }
        });
    }

    /// <summary>
    /// 18. 文章端點 - 結合 ETag 提升效率（根據文章範例實作）
    /// 展示如何結合 Cache-Control + ETag + must-revalidate 來優化 API 效能
    /// </summary>
    [HttpGet("article/{id}")]
    public async Task<IActionResult> GetArticle(int id, CancellationToken cancellationToken = default)
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        var article = await _repository.GetArticleAsync(id, cancellationToken);

        if (article == null)
        {
            return NotFound(new
            {
                requestId,
                message = $"找不到 ID 為 {id} 的文章"
            });
        }

        // 使用文章的 UpdatedAt 時間來生成 ETag
        var etag = $"\"{article.UpdatedAt.Ticks}\"";

        Response.Headers.CacheControl = "public, max-age=60, must-revalidate";
        Response.Headers.ETag = etag;

        // 檢查條件請求
        if (Request.Headers.IfNoneMatch == etag)
        {
            return StatusCode(304); // Not Modified - 節省流量
        }

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            article = new
            {
                article.Id,
                article.Title,
                article.Content,
                article.Author,
                article.CreatedAt,
                article.UpdatedAt,
                article.ViewCount,
                article.Tags
            },
            cacheInfo = new
            {
                etag,
                cacheControl = "public, max-age=60, must-revalidate",
                explanation = new
                {
                    flow = new[]
                    {
                        "1. 首次請求：伺服器回傳完整資料 + ETag",
                        "2. 60 秒內：瀏覽器直接使用快取（0 個請求到伺服器）",
                        "3. 60 秒後：瀏覽器發送條件請求（帶 If-None-Match）",
                        "4. 如果內容未改變：伺服器回傳 304（幾百位元組）",
                        "5. 如果內容改變：伺服器回傳 200 + 新資料 + 新 ETag"
                    },
                    benefits = new[]
                    {
                        "精確性：使用 UpdatedAt.Ticks 確保位元組級別的精確匹配",
                        "節省流量：304 回應通常只有幾百位元組",
                        "減少伺服器負載：伺服器只需比對 ETag，不需重新生成完整回應",
                        "提升使用者體驗：60 秒內的請求載入速度接近瞬間"
                    }
                }
            }
        });
    }

    /// <summary>
    /// 19. 更新文章 - 用於測試 ETag 變化
    /// 當文章更新後，UpdatedAt 會改變，導致 ETag 改變，瀏覽器會收到新的內容
    /// </summary>
    [HttpPut("article/{id}")]
    public async Task<IActionResult> UpdateArticle(int id, [FromBody] ArticleUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        var success = await _repository.UpdateArticleAsync(id, request.Title, request.Content, cancellationToken);

        if (!success)
        {
            return NotFound(new
            {
                requestId,
                message = $"找不到 ID 為 {id} 的文章"
            });
        }

        var updatedArticle = await _repository.GetArticleAsync(id, cancellationToken);
        var newETag = $"\"{updatedArticle!.UpdatedAt.Ticks}\"";

        return Ok(new
        {
            requestId,
            message = "文章已更新",
            newETag,
            article = new
            {
                updatedArticle.Id,
                updatedArticle.Title,
                updatedArticle.Content,
                updatedArticle.UpdatedAt
            },
            testInstructions = new
            {
                step1 = "再次請求 GET /api/clientcache/article/{id}",
                step2 = "注意 requestId 會增加，表示快取已失效",
                step3 = "ETag 已改變，瀏覽器會收到新的完整回應"
            }
        });
    }

    /// <summary>
    /// 20. 取得所有文章列表 - 展示集合資料的快取策略
    /// </summary>
    [HttpGet("articles")]
    public async Task<IActionResult> GetAllArticles(CancellationToken cancellationToken = default)
    {
        var requestId = Interlocked.Increment(ref _requestCounter);
        var articles = await _repository.GetAllArticlesAsync(cancellationToken);

        // 使用所有文章中最新的 UpdatedAt 作為 ETag
        var latestUpdate = articles.Max(a => a.UpdatedAt);
        var etag = $"\"{latestUpdate.Ticks}\"";

        Response.Headers.CacheControl = "public, max-age=300, must-revalidate";
        Response.Headers.ETag = etag;

        if (Request.Headers.IfNoneMatch == etag)
        {
            return StatusCode(304);
        }

        return Ok(new
        {
            requestId,
            timestamp = DateTime.UtcNow,
            totalCount = articles.Count(),
            articles = articles.Select(a => new
            {
                a.Id,
                a.Title,
                a.Author,
                a.CreatedAt,
                a.UpdatedAt,
                a.ViewCount,
                a.Tags
            }),
            cacheInfo = new
            {
                etag,
                cacheControl = "public, max-age=300, must-revalidate",
                description = "文章列表快取 5 分鐘。ETag 使用最新文章的更新時間，任何文章更新都會導致 ETag 改變"
            }
        });
    }

    private int? ExtractClientMaxAge(Microsoft.Extensions.Primitives.StringValues cacheControlHeaders)
    {
        foreach (var header in cacheControlHeaders)
        {
            if (header == null) continue;

            var parts = header.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("max-age="))
                {
                    if (int.TryParse(trimmed.Substring("max-age=".Length), out var maxAge))
                    {
                        return maxAge;
                    }
                }
            }
        }
        return null;
    }
}

/// <summary>
/// 文章更新請求模型
/// </summary>
public record ArticleUpdateRequest(string Title, string Content);
