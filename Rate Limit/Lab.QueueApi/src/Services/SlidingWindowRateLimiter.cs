using System.Collections.Concurrent;

namespace Lab.QueueApi.Services;

public class SlidingWindowRateLimiter : IRateLimiter
{
    private readonly ConcurrentQueue<DateTime> _requestTimestamps = new();
    private readonly int _maxRequests;
    private readonly TimeSpan _timeWindow;
    private readonly object _lock = new();

    public SlidingWindowRateLimiter(int maxRequests = 2, TimeSpan? timeWindow = null)
    {
        _maxRequests = maxRequests;
        _timeWindow = timeWindow ?? TimeSpan.FromSeconds(30);
    }

    public bool IsRequestAllowed()
    {
        lock (_lock)
        {
            CleanupOldRequests();
            return _requestTimestamps.Count < _maxRequests;
        }
    }

    public void RecordRequest()
    {
        lock (_lock)
        {
            _requestTimestamps.Enqueue(DateTime.UtcNow);
        }
    }

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

    private void CleanupOldRequests()
    {
        var cutoffTime = DateTime.UtcNow - _timeWindow;
        
        while (_requestTimestamps.TryPeek(out var timestamp) && timestamp < cutoffTime)
        {
            _requestTimestamps.TryDequeue(out _);
        }
    }
}

