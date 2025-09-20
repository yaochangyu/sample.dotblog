using System.Collections.Concurrent;

namespace Lab.QueueApi.Services;

/// <summary>
/// 使用滑動視窗演算法的速率限制器。
/// </summary>
public class SlidingWindowRateLimiter : IRateLimiter
{
    /// <summary>
    /// 儲存請求時間戳的並行佇列。
    /// </summary>
    private readonly ConcurrentQueue<DateTime> _requestTimestamps = new();

    /// <summary>
    /// 時間視窗內的最大請求數。
    /// </summary>
    private readonly int _maxRequests;

    /// <summary>
    /// 速率限制的時間視窗。
    /// </summary>
    private readonly TimeSpan _timeWindow;

    /// <summary>
    /// 用於鎖定以確保執行緒安全的物件。
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// 初始化 SlidingWindowRateLimiter 的新執行個體。
    /// </summary>
    /// <param name="maxRequests">時間視窗內的最大請求數。</param>
    /// <param name="timeWindow">速率限制的時間視窗。</param>
    public SlidingWindowRateLimiter(int maxRequests = 2, TimeSpan? timeWindow = null)
    {
        _maxRequests = maxRequests;
        _timeWindow = timeWindow ?? TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// 判斷目前是否允許新的請求。
    /// </summary>
    /// <returns>如果允許請求，則為 true；否則為 false。</returns>
    public bool IsRequestAllowed()
    {
        lock (_lock)
        {
            CleanupOldRequests();
            return _requestTimestamps.Count < _maxRequests;
        }
    }

    /// <summary>
    /// 記錄一個新的請求。
    /// </summary>
    public void RecordRequest()
    {
        lock (_lock)
        {
            _requestTimestamps.Enqueue(DateTime.UtcNow);
        }
    }

    /// <summary>
    /// 取得建議的重試延遲時間。
    /// </summary>
    /// <returns>一個 TimeSpan，表示客戶端應等待多長時間後再重試。</returns>
    public TimeSpan GetRetryAfter()
    {
        lock (_lock)
        {
            CleanupOldRequests();
            
            if (_requestTimestamps.Count < _maxRequests)
            {
                return TimeSpan.Zero;
            }

            // 找到最舊的請求時間，計算何時可以再次請求
            if (_requestTimestamps.TryPeek(out var oldestRequest))
            {
                var retryTime = oldestRequest.Add(_timeWindow);
                var retryAfter = retryTime - DateTime.UtcNow;
                return retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.Zero;
            }

            return _timeWindow;
        }
    }

    /// <summary>
    /// 清理佇列中早於目前時間視窗的過期請求。
    /// </summary>
    private void CleanupOldRequests()
    {
        var cutoffTime = DateTime.UtcNow - _timeWindow;
        
        while (_requestTimestamps.TryPeek(out var timestamp) && timestamp < cutoffTime)
        {
            _requestTimestamps.TryDequeue(out _);
        }
    }
}