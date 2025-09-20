using Lab.QueueApi.Commands;

namespace Lab.QueueApi.Services;

/// <summary>
/// 定義請求佇列提供者的介面。
/// </summary>
public interface IRequestQueueProvider
{
    /// <summary>
    /// 將請求非同步地加入佇列。
    /// </summary>
    /// <param name="requestData">請求的資料。</param>
    /// <returns>表示非同步操作的 Task，其結果為請求的唯一識別碼。</returns>
    Task<string> EnqueueCommandAsync(string requestData);

    /// <summary>
    /// 從佇列中非同步地取出請求。
    /// </summary>
    /// <param name="cancellationToken">用於取消操作的 CancellationToken。</param>
    /// <returns>表示非同步操作的 Task，其結果為從佇列中取出的 QueuedRequest，如果佇列已空或已完成，則為 null。</returns>
    Task<QueuedContext?> DequeueCommandAsync(CancellationToken cancellationToken = default);

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
    void CompleteCommand(string requestId, QueuedCommandResponse response);

    /// <summary>
    /// 取得目前佇列的長度。
    /// </summary>
    /// <returns>佇列中的請求數量。</returns>
    int GetQueueLength();

    /// <summary>
    /// 取得佇列中所有待處理的請求。
    /// </summary>
    /// <returns>包含所有待處理請求的集合。</returns>
    IEnumerable<QueuedContext> GetAllQueuedCommands();
}