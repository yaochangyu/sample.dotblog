namespace Lab.QueueApi.Models;

/// <summary>
/// 表示請求進入池時的回應資料
/// </summary>
public class PoolResponse
{
    /// <summary>
    /// 分配給請求的唯一識別碼
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// 建議的重試等待時間（秒）
    /// </summary>
    public int RetryAfterSeconds { get; set; }

    /// <summary>
    /// 給使用者的說明訊息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 回應的時間戳記
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}