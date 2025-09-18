using Lab.QueueApi.Models;

namespace Lab.QueueApi.Services;

/// <summary>
/// 請求池管理服務介面
/// </summary>
public interface IRequestPool
{
    /// <summary>
    /// 將請求加入池中
    /// </summary>
    /// <param name="request">要加入的請求</param>
    /// <returns>分配的 Request ID</returns>
    string AddRequest(ApiRequest request);

    /// <summary>
    /// 從池中取得下一個等待處理的請求
    /// </summary>
    /// <returns>下一個待處理的請求，如果池為空則返回 null</returns>
    PooledRequest? GetNextPendingRequest();

    /// <summary>
    /// 根據 Request ID 取得請求
    /// </summary>
    /// <param name="requestId">請求的唯一識別碼</param>
    /// <returns>對應的請求，如果不存在則返回 null</returns>
    PooledRequest? GetByRequestId(string requestId);

    /// <summary>
    /// 從池中移除指定的請求
    /// </summary>
    /// <param name="requestId">要移除的請求 ID</param>
    void RemoveRequest(string requestId);

    /// <summary>
    /// 檢查是否有等待中的請求
    /// </summary>
    /// <returns>如果有等待中的請求則返回 true</returns>
    bool HasPendingRequests();
}