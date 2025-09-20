using Lab.QueueApi.Commands;

namespace Lab.QueueApi.RateLimit;

/// <summary>
/// 佇列請求的狀態。
/// </summary>
public enum QueuedCommandStatus
{
    /// <summary>
    /// 在佇列中等待
    /// </summary>
    Queued,

    /// <summary>
    /// 已獲得許可，準備執行
    /// </summary>
    Ready,

    /// <summary>
    /// 正在執行中
    /// </summary>
    Processing,

    /// <summary>
    /// 已取消或失敗
    /// </summary>
    Failed,

    /// <summary>
    /// 已結束並從佇列移除
    /// </summary>
    Finished
}

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
    /// 請求狀態。
    /// </summary>
    public QueuedCommandStatus Status { get; set; } = QueuedCommandStatus.Queued;

    /// <summary>
    /// 狀態更新時間。
    /// </summary>
    public DateTime StatusUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 原始請求的資料。
    /// </summary>
    public object RequestData { get; set; }

    /// <summary>
    /// 用於等待請求完成的 TaskCompletionSource。
    /// </summary>
    public TaskCompletionSource<QueuedCommandResponse> CompletionSource { get; set; } = new();

    /// <summary>
    /// 更新請求狀態。
    /// </summary>
    /// <param name="newStatus">新的狀態</param>
    public void UpdateStatus(QueuedCommandStatus newStatus)
    {
        Status = newStatus;
        StatusUpdatedAt = DateTime.UtcNow;
    }
}