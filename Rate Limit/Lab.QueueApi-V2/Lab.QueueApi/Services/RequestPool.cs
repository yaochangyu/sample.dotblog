using System.Collections.Concurrent;
using System.Text.Json;
using Lab.QueueApi.Models;

namespace Lab.QueueApi.Services;

/// <summary>
/// 請求池管理服務實作
/// </summary>
public class RequestPool : IRequestPool
{
    /// <summary>
    /// 線程安全的請求字典
    /// </summary>
    private readonly ConcurrentDictionary<string, PooledRequest> _requests = new();

    /// <summary>
    /// 將請求加入池中
    /// </summary>
    /// <param name="request">要加入的請求</param>
    /// <returns>分配的 Request ID</returns>
    public string AddRequest(ApiRequest request)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var pooledRequest = new PooledRequest
        {
            RequestId = requestId,
            OriginalData = JsonSerializer.Serialize(request),
            CreatedTime = DateTime.UtcNow,
            Status = "Pending"
        };

        _requests[requestId] = pooledRequest;
        return requestId;
    }

    /// <summary>
    /// 從池中取得下一個等待處理的請求
    /// </summary>
    /// <returns>下一個待處理的請求，如果池為空則返回 null</returns>
    public PooledRequest? GetNextPendingRequest()
    {
        return _requests.Values
            .Where(r => r.Status == "Pending")
            .OrderBy(r => r.CreatedTime)
            .FirstOrDefault();
    }

    /// <summary>
    /// 根據 Request ID 取得請求
    /// </summary>
    /// <param name="requestId">請求的唯一識別碼</param>
    /// <returns>對應的請求，如果不存在則返回 null</returns>
    public PooledRequest? GetByRequestId(string requestId)
    {
        _requests.TryGetValue(requestId, out var request);
        return request;
    }

    /// <summary>
    /// 從池中移除指定的請求
    /// </summary>
    /// <param name="requestId">要移除的請求 ID</param>
    public void RemoveRequest(string requestId)
    {
        _requests.TryRemove(requestId, out _);
    }

    /// <summary>
    /// 檢查是否有等待中的請求
    /// </summary>
    /// <returns>如果有等待中的請求則返回 true</returns>
    public bool HasPendingRequests()
    {
        return _requests.Values.Any(r => r.Status == "Pending");
    }
}