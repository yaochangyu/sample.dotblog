namespace Lab.RateLimitQueueAPI.Models
{
    /// <summary>
    /// 表示佇列項目狀態的回應。
    /// </summary>
    public class QueueStatusResponse
    {
        /// <summary>
        /// 佇列項目的唯一識別碼。
        /// </summary>
        public string QueueId { get; set; } = string.Empty;
        
        /// <summary>
        /// 佇列項目的目前狀態。
        /// </summary>
        public QueueStatus Status { get; set; }
        
        /// <summary>
        /// 項目在佇列中的目前位置。
        /// </summary>
        public int Position { get; set; }
        
        /// <summary>
        /// 預估的等待時間 (秒)。
        /// </summary>
        public int EstimatedWaitTimeSeconds { get; set; }
        
        /// <summary>
        /// 要求的優先權。
        /// </summary>
        public int Priority { get; set; }
        
        /// <summary>
        /// 項目建立的時間戳記。
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
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
    }
}

