using Lab.QueueApi.Commands;

namespace Lab.QueueApi.Services;

/// <summary>
/// 定義請求佇列提供者的介面。
/// </summary>
public interface ICommandQueueProvider
{
    /// <summary>
    /// 將請求非同步地加入佇列。
    /// </summary>
    /// <param name="requestData">請求的資料。</param>
    /// <param name="cancel"></param>
    /// <returns>表示非同步操作的 Task，其結果為請求的唯一識別碼。</returns>
    Task<string> EnqueueCommandAsync(object requestData, CancellationToken cancel = default);

    /// <summary>
    /// 從佇列中非同步地取出請求。
    /// </summary>
    /// <param name="cancel">用於取消操作的 CancellationToken。</param>
    /// <returns>表示非同步操作的 Task，其結果為從佇列中取出的 QueuedRequest，如果佇列已空或已完成，則為 null。</returns>
    Task<QueuedContext?> DequeueCommandAsync(CancellationToken cancel = default);

    /// <summary>
    /// 取得下一個等待中的請求（不從佇列移除）。
    /// </summary>
    /// <param name="cancel">用於取消操作的 CancellationToken。</param>
    /// <returns>表示非同步操作的 Task，其結果為下一個等待中的請求，如果沒有則為 null。</returns>
    Task<QueuedContext?> GetNextQueuedRequestAsync(CancellationToken cancel = default);

    /// <summary>
    /// 標記請求為準備執行狀態。
    /// </summary>
    /// <param name="requestId">請求的唯一識別碼。</param>
    /// <param name="cancel">用於取消操作的 CancellationToken。</param>
    /// <returns>表示非同步操作的 Task。</returns>
    Task MarkRequestAsReadyAsync(string requestId, CancellationToken cancel = default);

    /// <summary>
    /// 執行準備好的請求並標記為處理中。
    /// </summary>
    /// <param name="requestId">請求的唯一識別碼。</param>
    /// <param name="cancel">用於取消操作的 CancellationToken。</param>
    /// <returns>表示非同步操作的 Task，其結果為請求上下文，如果請求不存在或狀態不正確則為 null。</returns>
    Task<QueuedContext?> ExecuteReadyRequestAsync(string requestId, CancellationToken cancel = default);

    /// <summary>
    /// 將請求標記為已結束並從佇列中移除。
    /// </summary>
    /// <param name="request">請求的唯一識別碼。</param>
    /// <param name="response">最終回應。</param>
    /// <param name="cancel">用於取消操作的 CancellationToken。</param>
    /// <returns>表示非同步操作的 Task。</returns>
    Task FinishAndRemoveRequestAsync(QueuedContext request, QueuedCommandResponse response, CancellationToken cancel = default);

    /// <summary>
    /// 非同步地等待特定請求的回應。
    /// </summary>
    /// <param name="requestId">要等待的請求的唯一識別碼。</param>
    /// <param name="timeout">等待的逾時時間。</param>
    /// <returns>表示非同步操作的 Task，其結果為 ApiResponse。</returns>
    Task<QueuedCommandResponse> WaitForResponseAsync(string requestId, TimeSpan timeout);

    /// <summary>
    /// 標示一個請求已完成，並設定其回應。
    /// </summary>
    /// <param name="requestId">已完成的請求的唯一識別碼。</param>
    /// <param name="response">請求的 ApiResponse。</param>
    /// <param name="cancel"></param>
    Task CompleteCommandAsync(string requestId, QueuedCommandResponse response,CancellationToken cancel = default);

    /// <summary>
    /// 取得目前佇列的長度。
    /// </summary>
    /// <returns>佇列中的請求數量。</returns>
    int GetQueueLength();

    /// <summary>
    /// 取得佇列中所有待處理的請求。
    /// </summary>
    /// <param name="cancel">用於取消操作的 CancellationToken。</param>
    /// <returns>包含所有待處理請求的集合。</returns>
    Task<IEnumerable<QueuedContext>> GetAllQueuedCommandsAsync(CancellationToken cancel = default);

    /// <summary>
    /// 檢查佇列是否已滿。
    /// </summary>
    /// <returns>如果佇列已滿則返回 true，否則返回 false。</returns>
    bool IsQueueFull();

    /// <summary>
    /// 取得佇列的最大容量。
    /// </summary>
    /// <returns>佇列的最大容量。</returns>
    int GetMaxCapacity();

    /// <summary>
    /// 清理超過指定時間且未被取得結果的過期請求。
    /// </summary>
    /// <param name="maxAge">請求最大存活時間。</param>
    /// <returns>被清理的請求數量。</returns>
    int CleanupExpiredRequests(TimeSpan maxAge);

    /// <summary>
    /// 取得清理記錄摘要。
    /// </summary>
    /// <returns>清理記錄摘要。</returns>
    Task<CleanupSummaryResponse> GetCleanupSummaryAsync(CancellationToken cancel = default);
}