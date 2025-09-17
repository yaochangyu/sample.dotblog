using Lab.RateLimitQueueAPI.Models;

namespace Lab.RateLimitQueueAPI.Services
{
    /// <summary>
    /// 定義佇列處理常式應實作的方法。
    /// </summary>
    public interface IQueueHandler
    {
        /// <summary>
        /// 將項目以非同步方式加入佇列。
        /// </summary>
        /// <param name="item">要加入佇列的項目。</param>
        /// <returns>表示非同步作業的工作。工作結果包含佇列項目的唯一識別碼。</returns>
        Task<string> EnqueueAsync(QueueItem item);

        /// <summary>
        /// 從佇列中以非同步方式取出項目。
        /// </summary>
        /// <returns>表示非同步作業的工作。工作結果包含佇列中的下一個項目，如果佇列為空則為 null。</returns>
        Task<QueueItem?> DequeueAsync();

        /// <summary>
        /// 以非同步方式取得佇列項目的狀態。
        /// </summary>
        /// <param name="queueId">佇列項目的唯一識別碼。</param>
        /// <returns>表示非同步作業的工作。工作結果包含佇列項目的狀態，如果找不到則為 null。</returns>
        Task<QueueStatusResponse?> GetQueueStatusAsync(string queueId);

        /// <summary>
        /// 以非同步方式更新佇列項目的狀態。
        /// </summary>
        /// <param name="queueId">佇列項目的唯一識別碼。</param>
        /// <param name="status">新的狀態。</param>
        /// <param name="result">處理結果。</param>
        /// <param name="errorMessage">錯誤訊息。</param>
        /// <returns>表示非同步作業的工作。</returns>
        Task UpdateQueueItemStatusAsync(string queueId, QueueStatus status, string? result = null, string? errorMessage = null);

        /// <summary>
        /// 以非同步方式取得佇列的目前長度。
        /// </summary>
        /// <returns>表示非同步作業的工作。工作結果包含佇列中的項目數。</returns>
        Task<int> GetQueueLengthAsync();

        /// <summary>
        /// 以非同步方式檢查佇列是否已滿。
        /// </summary>
        /// <returns>表示非同步作業的工作。如果佇列已滿，工作結果為 true，否則為 false。</returns>
        Task<bool> IsQueueFullAsync();

        /// <summary>
        /// 以非同步方式清理已過期的項目。
        /// </summary>
        /// <returns>表示非同步作業的工作。</returns>
        Task CleanupExpiredItemsAsync();
    }
}

