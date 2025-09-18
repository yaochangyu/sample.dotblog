namespace Lab.QueueApi.Models;

/// <summary>
/// 表示在請求池中等待處理的請求項目
/// </summary>
public class PooledRequest
{
    /// <summary>
    /// 請求的唯一識別碼
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// 原始請求資料的 JSON 序列化字串
    /// </summary>
    public string OriginalData { get; set; } = string.Empty;

    /// <summary>
    /// 請求創建的 UTC 時間
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 請求的當前狀態 (Pending, Processing, Completed, Failed, UnProcessed, Canceled)
    /// </summary>
    public string Status { get; set; } = "Pending";
}