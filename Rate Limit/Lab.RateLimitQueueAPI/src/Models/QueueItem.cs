namespace Lab.RateLimitQueueAPI.Models
{
    /// <summary>
    /// 表示佇列中的一個項目。
    /// </summary>
    public class QueueItem
    {
        /// <summary>
        /// 佇列項目的唯一識別碼。
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// 原始要求的路徑。
        /// </summary>
        public string RequestPath { get; set; } = string.Empty;
        
        /// <summary>
        /// 原始要求的 HTTP 方法。
        /// </summary>
        public string HttpMethod { get; set; } = string.Empty;
        
        /// <summary>
        /// 原始要求的標頭。
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new();
        
        /// <summary>
        /// 原始要求的主體。
        /// </summary>
        public string Body { get; set; } = string.Empty;
        
        /// <summary>
        /// 項目建立的時間戳記。
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 佇列項目的目前狀態。
        /// </summary>
        public QueueStatus Status { get; set; } = QueueStatus.Queued;
        
        /// <summary>
        /// 要求的優先權 (0 = 正常，較高的數字 = 較高的優先權)。
        /// </summary>
        public int Priority { get; set; } = 0; 
        
        /// <summary>
        /// 項目處理的時間戳記。
        /// </summary>
        public DateTime? ProcessedAt { get; set; }
        
        /// <summary>
        /// 處理結果。
        /// </summary>
        public string? Result { get; set; }
        
        /// <summary>
        /// 如果處理失敗，則為錯誤訊息。
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// 項目的重試次數。
        /// </summary>
        public int RetryCount { get; set; } = 0;
        
        /// <summary>
        /// 項目的到期時間。
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// 表示佇列項目狀態的列舉。
    /// </summary>
    public enum QueueStatus
    {
        /// <summary>
        /// 項目已排入佇列。
        /// </summary>
        Queued,
        
        /// <summary>
        /// 項目正在處理中。
        /// </summary>
        Processing,
        
        /// <summary>
        /// 項目已成功處理。
        /// </summary>
        Completed,
        
        /// <summary>
        /// 項目處理失敗。
        /// </summary>
        Failed,
        
        /// <summary>
        /// 項目已過期。
        /// </summary>
        Expired
    }
}

