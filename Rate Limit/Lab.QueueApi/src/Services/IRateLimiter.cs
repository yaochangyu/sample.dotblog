namespace Lab.QueueApi.Services;

/// <summary>
/// 定義速率限制器的介面。
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// 判斷目前是否允許新的請求。
    /// </summary>
    /// <returns>如果允許請求，則為 true；否則為 false。</returns>
    bool IsRequestAllowed();

    /// <summary>
    /// 記錄一個新的請求。
    /// </summary>
    void RecordRequest();

    /// <summary>
    /// 取得建議的重試延遲時間。
    /// </summary>
    /// <returns>一個 TimeSpan，表示客戶端應等待多長時間後再重試。</returns>
    TimeSpan GetRetryAfter();
}