using System.Collections.Concurrent;
using System.Threading.Channels;
using Lab.RateLimitQueueAPI.Models;

namespace Lab.RateLimitQueueAPI.Services
{
    /// <summary>
    /// 處理先進先出 (FIFO) 佇列的服務。
    /// </summary>
    public class FifoQueueHandler : IQueueHandler
    {
        /// <summary>
        /// 用於儲存佇列項目的通道。
        /// </summary>
        private readonly Channel<QueueItem> _queue;
        
        /// <summary>
        /// 用於追蹤佇列中所有項目的並行字典。
        /// </summary>
        private readonly ConcurrentDictionary<string, QueueItem> _queueItems;
        
        /// <summary>
        /// 用於記錄日誌的記錄器。
        /// </summary>
        private readonly ILogger<FifoQueueHandler> _logger;
        
        /// <summary>
        /// 佇列的最大大小。
        /// </summary>
        private readonly int _maxQueueSize;
        
        /// <summary>
        /// 佇列項目的逾時時間。
        /// </summary>
        private readonly TimeSpan _itemTimeout;

        /// <summary>
        /// 初始化 FifoQueueHandler 的新執行個體。
        /// </summary>
        /// <param name="logger">記錄器。</param>
        /// <param name="configuration">應用程式組態。</param>
        public FifoQueueHandler(ILogger<FifoQueueHandler> logger, IConfiguration configuration)
        {
            _logger = logger;
            _maxQueueSize = configuration.GetValue<int>("Queue:MaxSize", 100);
            _itemTimeout = TimeSpan.FromSeconds(configuration.GetValue<int>("Queue:TimeoutSeconds", 300));
            
            var options = new BoundedChannelOptions(_maxQueueSize)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };
            
            _queue = Channel.CreateBounded<QueueItem>(options);
            _queueItems = new ConcurrentDictionary<string, QueueItem>();
        }

        /// <summary>
        /// 將項目加入佇列。
        /// </summary>
        /// <param name="item">要加入佇列的項目。</param>
        /// <returns>佇列項目的唯一識別碼。</returns>
        public async Task<string> EnqueueAsync(QueueItem item)
        {
            item.ExpiresAt = DateTime.UtcNow.Add(_itemTimeout);
            
            if (!await _queue.Writer.WaitToWriteAsync())
            {
                throw new InvalidOperationException("Queue is closed");
            }

            _queueItems[item.Id] = item;
            await _queue.Writer.WriteAsync(item);
            
            _logger.LogInformation("Item {QueueId} enqueued at {Timestamp}", item.Id, item.CreatedAt);
            return item.Id;
        }

        /// <summary>
        /// 從佇列中取出項目。
        /// </summary>
        /// <returns>佇列中的下一個項目，如果佇列為空則為 null。</returns>
        public async Task<QueueItem?> DequeueAsync()
        {
            if (await _queue.Reader.WaitToReadAsync())
            {
                if (_queue.Reader.TryRead(out var item))
                {
                    if (item.ExpiresAt.HasValue && DateTime.UtcNow > item.ExpiresAt.Value)
                    {
                        // Item has expired
                        await UpdateQueueItemStatusAsync(item.Id, QueueStatus.Expired);
                        _logger.LogWarning("Item {QueueId} expired", item.Id);
                        return await DequeueAsync(); // Try next item
                    }

                    await UpdateQueueItemStatusAsync(item.Id, QueueStatus.Processing);
                    return item;
                }
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
            var estimatedWaitTime = position * 1; // Assume 1 second per item processing time

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
                _logger.LogInformation("Item {QueueId} status updated to {Status}", queueId, status);
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
                _logger.LogInformation("Expired item {QueueId} removed", item.Id);
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
            var queuedItems = _queueItems.Values
                .Where(x => x.Status == QueueStatus.Queued)
                .OrderBy(x => x.CreatedAt)
                .ToList();

            var position = queuedItems.FindIndex(x => x.Id == queueId);
            return position >= 0 ? position + 1 : 0;
        }
    }
}

