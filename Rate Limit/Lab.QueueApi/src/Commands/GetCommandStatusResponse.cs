namespace Lab.QueueApi.Commands;

/// <summary>
/// 代表佇列任務的
/// </summary>
public class GetCommandStatusResponse
{
    /// <summary>
    /// 請求的唯一識別碼。
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 請求進入佇列的時間。
    /// </summary>
    public DateTime QueuedAt { get; set; }

    /// <summary>
    /// 原始請求的資料。
    /// </summary>
    public object RequestData { get; set; }

    /// <summary>
    /// 在佇列中等待的時間。
    /// </summary>
    public TimeSpan WaitingTime { get; set; }
}