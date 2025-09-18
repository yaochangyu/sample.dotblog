namespace Lab.QueueApi.Services;

/// <summary>
/// 滑動視窗速率限制器實作
/// </summary>
public class SlidingWindowRateLimiter : IRateLimitService
{
    /// <summary>
    /// 請求時間記錄清單
    /// </summary>
    private readonly List<DateTime> _requestTimes = new();

    /// <summary>
    /// 最大請求數量
    /// </summary>
    private readonly int _maxRequests;

    /// <summary>
    /// 時間視窗大小
    /// </summary>
    private readonly TimeSpan _timeWindow;

    /// <summary>
    /// 同步鎖物件
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// 建構子
    /// </summary>
    /// <param name="maxRequests">時間視窗內最大請求數</param>
    /// <param name="timeWindow">時間視窗大小</param>
    public SlidingWindowRateLimiter(int maxRequests = 2, TimeSpan? timeWindow = null)
    {
        _maxRequests = maxRequests;
        _timeWindow = timeWindow ?? TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// 檢查是否可以處理新請求
    /// </summary>
    /// <returns>如果可以處理則返回 true，否則返回 false</returns>
    public bool CanProcess()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;

            // 清理超過時間視窗的記錄
            _requestTimes.RemoveAll(time => now - time > _timeWindow);

            return _requestTimes.Count < _maxRequests;
        }
    }

    /// <summary>
    /// 記錄一個新的請求處理
    /// </summary>
    public void RecordRequest()
    {
        lock (_lock)
        {
            _requestTimes.Add(DateTime.UtcNow);
        }
    }
}