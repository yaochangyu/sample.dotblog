using System.Collections.Concurrent;
using Lab.RateLimitQueueAPI.Models;

namespace Lab.RateLimitQueueAPI.Services
{
    /// <summary>
    /// 處理優先權佇列的服務。
    /// </summary>
    public class PriorityQueueHandler : IQueueHandler
    {
        /// <summary>
        /// 優先權佇列，用於儲存佇列項目。
        /// </summary>
        private readonly PriorityQueue<QueueItem, (int Priority, DateTime CreatedAt)> _queue;
        
        /// <summary>
        /// 用於追蹤佇列中所有項目的並行字典。
        /// </summary>
        private readonly ConcurrentDictionary<string, QueueItem> _queueItems;
        
        /// <summary>
        /// 用於記錄日誌的記錄器。
        /// </summary>
        private readonly ILogger<PriorityQueueHandler> _logger;
        
        /// <summary>
        /// 佇列的最大大小。
        /// </summary>
        private readonly int _maxQueueSize;
        
        /// <summary>
        /// 佇列項目的逾時時間。
        /// </summary>
        private readonly TimeSpan _itemTimeout;
        
        /// <summary>
        /// 用於鎖定佇列的物件。
        /// </summary>
        private readonly object _queueLock = new object();

        /// <summary>
        /// 初始化 PriorityQueueHandler 的新執行個體。
        /// </summary>
        /// <param name="logger">記錄器。</param>
        /// <param name="configuration">應用程式組態。</param>
        public PriorityQueueHandler(ILogger<PriorityQueueHandler> logger, IConfiguration configuration)
        {
            _logger = logger;
            _maxQueueSize = configuration.GetValue<int>("Queue:MaxSize", 100);
            _itemTimeout = TimeSpan.FromSeconds(configuration.GetValue<int>("Queue:TimeoutSeconds", 300));
            
            _queue = new PriorityQueue<QueueItem, (int Priority, DateTime CreatedAt)>();
            _queueItems = new ConcurrentDictionary<string, QueueItem>();
        }

        /// <summary>
        /// 將項目加入佇列。
        /// </summary>
        /// <param name="item">要加入佇列的項目。</param>
        /// <returns>佇列項目的唯一識別碼。</returns>
        public async Task<string> EnqueueAsync(QueueItem item)
        {
            if (await IsQueueFullAsync())
            {
                throw new InvalidOperationException("Queue is full");
            }

            item.ExpiresAt = DateTime.UtcNow.Add(_itemTimeout);
            
            lock (_queueLock)
            {
                // 較高的優先權數字表示較高的優先權，但 PriorityQueue 是最小堆積
                // 所以我們對優先權取反，並使用 CreatedAt 作為次要排序
                var priority = (-item.Priority, item.CreatedAt);
                _queue.Enqueue(item, priority);
            }
            
            _queueItems[item.Id] = item;
            
            _logger.LogInformation("項目 {QueueId} 已加入佇列，優先權為 {Priority}，時間為 {Timestamp}", 
                item.Id, item.Priority, item.CreatedAt);
            
            return item.Id;
        }

        /// <summary>
        /// 從佇列中取出項目。
        /// </summary>
        /// <returns>佇列中的下一個項目，如果佇列為空則為 null。</returns>
        public async Task<QueueItem?> DequeueAsync()
        {
            QueueItem? item = null;
            
            lock (_queueLock)
            {
                while (_queue.Count > 0)
                {
                    item = _queue.Dequeue();
                    
                    // 檢查項目是否已過期
                    if (item.ExpiresAt.HasValue && DateTime.UtcNow > item.ExpiresAt.Value)
                    {
                        _ = Task.Run(() => UpdateQueueItemStatusAsync(item.Id, QueueStatus.Expired));
                        _logger.LogWarning("項目 {QueueId} 已過期", item.Id);
                        item = null;
                        continue;
                    }
                    
                    break;
                }
            }

            if (item != null)
            {
                await UpdateQueueItemStatusAsync(item.Id, QueueStatus.Processing);
                return item;
            }

            return null;
        }

        /// <summary>
        /// 取得佇列項目的狀態。
        /// </summary>
        /// <param name="queueId">佇列項目的唯一識別碼。</param>
        /// <returns>佇列項目的狀態，如果找不到則為 null。</returns>
        public async Task<QueueStatusResponse?> GetQueueStatusAsync(string queueId)
        {
            if (!_queueItems.TryGetValue(queueId, out var item))
            {
                return null;
            }

            var position = await GetPositionInQueueAsync(queueId);
            var estimatedWaitTime = CalculateEstimatedWaitTime(item.Priority, position);

            return new QueueStatusResponse
            {
                QueueId = queueId,
                Status = item.Status,
                Position = position,
                EstimatedWaitTimeSeconds = estimatedWaitTime,
                Priority = item.Priority,
                CreatedAt = item.CreatedAt,
                ProcessedAt = item.ProcessedAt,
                Result = item.Result,
                ErrorMessage = item.ErrorMessage
            };
        }

        /// <summary>
        /// 更新佇列項目的狀態。
        /// </summary>
        /// <param name="queueId">佇列項目的唯一識別碼。</param>
        /// <param name="status">新的狀態。</param>
        /// <param name="result">處理結果。</param>
        /// <param name="errorMessage">錯誤訊息。</param>
        public async Task UpdateQueueItemStatusAsync(string queueId, QueueStatus status, string? result = null, string? errorMessage = null)
        {
            if (_queueItems.TryGetValue(queueId, out var item))
            {
                item.Status = status;
                item.Result = result;
                item.ErrorMessage = errorMessage;
                
                if (status == QueueStatus.Processing)
                {
                    item.ProcessedAt = DateTime.UtcNow;
                }

                _queueItems[queueId] = item;
                _logger.LogInformation("項目 {QueueId} 的狀態已更新為 {Status}", queueId, status);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// 取得佇列的目前長度。
        /// </summary>
        /// <returns>佇列中的項目數。</returns>
        public async Task<int> GetQueueLengthAsync()
        {
            await Task.CompletedTask;
            return _queueItems.Count(x => x.Value.Status == QueueStatus.Queued);
        }

        /// <summary>
        /// 檢查佇列是否已滿。
        /// </summary>
        /// <returns>如果佇列已滿則為 true，否則為 false。</returns>
        public async Task<bool> IsQueueFullAsync()
        {
            var queueLength = await GetQueueLengthAsync();
            return queueLength >= _maxQueueSize;
        }

        /// <summary>
        /// 清理已過期的項目。
        /// </summary>
        public async Task CleanupExpiredItemsAsync()
        {
            var expiredItems = _queueItems.Values
                .Where(x => x.ExpiresAt.HasValue && DateTime.UtcNow > x.ExpiresAt.Value)
                .ToList();

            foreach (var item in expiredItems)
            {
                await UpdateQueueItemStatusAsync(item.Id, QueueStatus.Expired);
                _queueItems.TryRemove(item.Id, out _);
                _logger.LogInformation("已移除過期的項目 {QueueId}", item.Id);
            }
        }

        /// <summary>
        /// 取得項目在佇列中的位置。
        /// </summary>
        /// <param name="queueId">佇列項目的唯一識別碼。</param>
        /// <returns>項目在佇列中的位置。</returns>
        private async Task<int> GetPositionInQueueAsync(string queueId)
        {
            await Task.CompletedTask;
            
            if (!_queueItems.TryGetValue(queueId, out var targetItem) || targetItem.Status != QueueStatus.Queued)
            {
                return 0;
            }

            var queuedItems = _queueItems.Values
                .Where(x => x.Status == QueueStatus.Queued)
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.CreatedAt)
                .ToList();

            var position = queuedItems.FindIndex(x => x.Id == queueId);
            return position >= 0 ? position + 1 : 0;
        }

        /// <summary>
        /// 計算預估的等待時間。
        /// </summary>
        /// <param name="priority">要求的優先權。</param>
        /// <param name="position">要求在佇列中的位置。</param>
        /// <returns>預估的等待時間 (秒)。</returns>
        private int CalculateEstimatedWaitTime(int priority, int position)
        {
            // 基本等待時間為每個位置 1 秒
            var baseWaitTime = position * 1;
            
            // 根據優先權進行調整 (較高的優先權等待時間較短)
            var priorityMultiplier = Math.Max(0.1, 1.0 - (priority * 0.1));
            
            return (int)(baseWaitTime * priorityMultiplier);
        }
    }
}

