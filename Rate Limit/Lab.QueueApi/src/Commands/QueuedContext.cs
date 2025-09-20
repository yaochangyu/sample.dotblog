namespace Lab.QueueApi.Commands;

/// <summary>
/// 代表一個已排入佇列的請求。
/// </summary>
public class QueuedContext
{
    /// <summary>
    /// 請求的唯一識別碼。
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 請求進入佇列的時間。
    /// </summary>
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 原始請求的資料。
    /// </summary>
    public object RequestData { get; set; }

    /// <summary>
    /// 用於等待請求完成的 TaskCompletionSource。
    /// </summary>
    public TaskCompletionSource<QueuedCommandResponse> CompletionSource { get; set; } = new();
}