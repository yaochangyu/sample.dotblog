namespace Lab.QueueApi.Commands;

/// <summary>
/// 代表一個被清理的請求記錄。
/// </summary>
public class CleanupRecord
{
    /// <summary>
    /// 被清理的請求 ID。
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// 原始請求的資料。
    /// </summary>
    public object RequestData { get; set; } = new();

    /// <summary>
    /// 請求進入佇列的時間。
    /// </summary>
    public DateTime QueuedAt { get; set; }

    /// <summary>
    /// 請求被清理的時間。
    /// </summary>
    public DateTime CleanedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 請求在佇列中存活的時間。
    /// </summary>
    public TimeSpan LifeSpan => CleanedAt - QueuedAt;

    /// <summary>
    /// 清理原因。
    /// </summary>
    public string Reason { get; set; } = "Expired";
}