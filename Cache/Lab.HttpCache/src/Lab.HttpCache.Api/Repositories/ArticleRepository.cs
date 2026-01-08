using Lab.HttpCache.Api.Models;
using Lab.HttpCache.Api.Providers;

namespace Lab.HttpCache.Api.Repositories;

/// <summary>
/// 記憶體內的文章資料倉儲（模擬資料庫）
/// 使用 HybridCache 實作伺服器端快取
/// </summary>
public class ArticleRepository : IArticleRepository
{
    // 靜態字典模擬資料庫（所有實例共享）
    private static readonly Dictionary<int, Article> _articles;
    private readonly ICacheProvider _cacheProvider;
    private static readonly DateTime BaseTime = DateTime.UtcNow.AddDays(-7);

    // 快取鍵常數
    private const string ArticleCacheKeyPrefix = "article:";
    private const string AllArticlesCacheKey = "articles:all";
    private const string ArticleTag = "articles";

    // 靜態建構子初始化模擬資料（只執行一次）
    static ArticleRepository()
    {
        _articles = new Dictionary<int, Article>
        {
            {
                1, new Article
                {
                    Id = 1,
                    Title = "深入探討 HTTP Client-Side Cache",
                    Content = "HTTP 快取是一個強大但經常被忽視的效能優化工具。透過正確使用 Cache-Control 指令，我們可以大幅降低伺服器負載...",
                    Author = "技術部落格",
                    CreatedAt = BaseTime,
                    UpdatedAt = BaseTime,
                    ViewCount = 1250,
                    Tags = new List<string> { "HTTP", "Cache", "效能優化", "RFC 9111" }
                }
            },
            {
                2, new Article
                {
                    Id = 2,
                    Title = "ASP.NET Core 的 HybridCache 實戰",
                    Content = ".NET 9 引入了全新的 HybridCache 機制，結合了記憶體快取和分散式快取的優勢...",
                    Author = "技術部落格",
                    CreatedAt = BaseTime.AddDays(1),
                    UpdatedAt = BaseTime.AddDays(1),
                    ViewCount = 980,
                    Tags = new List<string> { "ASP.NET Core", "HybridCache", ".NET 9", "快取" }
                }
            },
            {
                3, new Article
                {
                    Id = 3,
                    Title = "ETag 與條件請求的最佳實踐",
                    Content = "ETag 提供了一種精確的快取驗證機制，可以有效節省網路流量。本文將深入探討 ETag 的運作原理...",
                    Author = "技術部落格",
                    CreatedAt = BaseTime.AddDays(2),
                    UpdatedAt = BaseTime.AddDays(2),
                    ViewCount = 756,
                    Tags = new List<string> { "ETag", "HTTP", "條件請求", "效能" }
                }
            },
            {
                4, new Article
                {
                    Id = 4,
                    Title = "CDN 與 s-maxage 的應用",
                    Content = "當使用 CDN 加速時，s-maxage 指令可以為共享快取設定不同的快取時間，與瀏覽器快取分開管理...",
                    Author = "技術部落格",
                    CreatedAt = BaseTime.AddDays(3),
                    UpdatedAt = BaseTime.AddDays(3),
                    ViewCount = 645,
                    Tags = new List<string> { "CDN", "Cache-Control", "s-maxage", "分散式系統" }
                }
            },
            {
                5, new Article
                {
                    Id = 5,
                    Title = "stale-while-revalidate 優化使用者體驗",
                    Content = "這個指令允許瀏覽器在快取過期後立即回傳舊的快取內容，同時在背景重新驗證，達到最佳的使用者體驗...",
                    Author = "技術部落格",
                    CreatedAt = BaseTime.AddDays(4),
                    UpdatedAt = BaseTime.AddDays(4),
                    ViewCount = 523,
                    Tags = new List<string> { "stale-while-revalidate", "UX", "效能優化", "RFC 5861" }
                }
            }
        };
    }

    // 實例建構子
    public ArticleRepository(ICacheProvider cacheProvider)
    {
        _cacheProvider = cacheProvider;
    }

    /// <summary>
    /// 取得文章（使用 HybridCache 快取）
    /// </summary>
    public async Task<Article?> GetArticleAsync(int id, CancellationToken cancellationToken = default)
    {
        // 使用 HybridCache 快取文章資料
        // 這展示了伺服器端快取（HybridCache）與客戶端快取（HTTP Cache-Control）的結合
        var cacheKey = $"{ArticleCacheKeyPrefix}{id}";

        var article = await _cacheProvider.GetOrCreateAsync(
            key: cacheKey,
            factory: async ct =>
            {
                // 模擬從資料庫讀取（這裡實際上是從記憶體字典讀取）
                await Task.Delay(10, ct); // 模擬資料庫延遲
                return _articles.TryGetValue(id, out var result) ? result : null;
            },
            expiration: TimeSpan.FromMinutes(5), // 伺服器端快取 5 分鐘
            tags: new[] { ArticleTag, $"{ArticleTag}:{id}" }
        );

        return article;
    }

    /// <summary>
    /// 取得所有文章（使用 HybridCache 快取）
    /// </summary>
    public async Task<IEnumerable<Article>> GetAllArticlesAsync(CancellationToken cancellationToken = default)
    {
        // 使用 HybridCache 快取文章列表
        var articles = await _cacheProvider.GetOrCreateAsync(
            key: AllArticlesCacheKey,
            factory: async ct =>
            {
                // 模擬從資料庫讀取
                await Task.Delay(20, ct); // 模擬資料庫延遲
                return _articles.Values.OrderByDescending(a => a.CreatedAt).ToList();
            },
            expiration: TimeSpan.FromMinutes(3), // 列表快取時間較短
            tags: new[] { ArticleTag }
        );

        return articles ?? Enumerable.Empty<Article>();
    }

    /// <summary>
    /// 更新文章（會清除相關快取）
    /// </summary>
    public async Task<bool> UpdateArticleAsync(int id, string title, string content, CancellationToken cancellationToken = default)
    {
        if (!_articles.TryGetValue(id, out var article))
        {
            return false;
        }

        // 更新資料
        article.Title = title;
        article.Content = content;
        article.UpdatedAt = DateTime.UtcNow; // 更新時間，會導致 ETag 改變

        // 清除相關的快取
        // 1. 清除單一文章快取
        var cacheKey = $"{ArticleCacheKeyPrefix}{id}";
        await _cacheProvider.RemoveAsync(cacheKey);

        // 2. 清除文章列表快取（因為列表中的 UpdatedAt 會影響 ETag）
        await _cacheProvider.RemoveAsync(AllArticlesCacheKey);

        // 也可以使用 Tag 來清除所有相關快取：
        // await _cacheProvider.RemoveByTagAsync($"{ArticleTag}:{id}");

        return true;
    }
}
