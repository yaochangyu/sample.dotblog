namespace Lab.HttpCache.Api.Models;

/// <summary>
/// 文章模型
/// </summary>
public class Article
{
    /// <summary>
    /// 文章 ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 文章標題
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 文章內容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 作者
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新時間（用於生成 ETag）
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 觀看次數
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// 標籤
    /// </summary>
    public List<string> Tags { get; set; } = new();
}
