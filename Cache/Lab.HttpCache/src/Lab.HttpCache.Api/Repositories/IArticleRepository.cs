using Lab.HttpCache.Api.Models;

namespace Lab.HttpCache.Api.Repositories;

/// <summary>
/// 文章資料倉儲介面
/// </summary>
public interface IArticleRepository
{
    /// <summary>
    /// 根據 ID 取得文章
    /// </summary>
    /// <param name="id">文章 ID</param>
    /// <returns>文章物件，若不存在則回傳 null</returns>
    Article? GetArticle(int id);

    /// <summary>
    /// 取得所有文章
    /// </summary>
    /// <returns>文章清單</returns>
    IEnumerable<Article> GetAllArticles();

    /// <summary>
    /// 更新文章（模擬更新，會改變 UpdatedAt 時間）
    /// </summary>
    /// <param name="id">文章 ID</param>
    /// <param name="title">新標題</param>
    /// <param name="content">新內容</param>
    /// <returns>是否更新成功</returns>
    bool UpdateArticle(int id, string title, string content);
}
