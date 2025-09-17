using System.Collections.Concurrent;

namespace Lab.RateLimitQueueAPI.Services
{
    /// <summary>
    /// 處理適應性延遲的服務，用於動態計算重試時間。
    /// </summary>
    public class AdaptiveDelayHandler
    {
        /// <summary>
        /// 用於記錄日誌的記錄器。
        /// </summary>
        private readonly ILogger<AdaptiveDelayHandler> _logger;
        
        /// <summary>
        /// 追蹤每個用戶端的重試次數。
        /// </summary>
        private readonly ConcurrentDictionary<string, int> _clientRetryCount;
        
        /// <summary>
        /// 追蹤每個用戶端的上次請求時間。
        /// </summary>
        private readonly ConcurrentDictionary<string, DateTime> _clientLastRequest;
        
        /// <summary>
        /// 應用程式組態。
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// 初始化 AdaptiveDelayHandler 的新執行個體。
        /// </summary>
        /// <param name="logger">記錄器。</param>
        /// <param name="configuration">應用程式組態。</param>
        public AdaptiveDelayHandler(ILogger<AdaptiveDelayHandler> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _clientRetryCount = new ConcurrentDictionary<string, int>();
            _clientLastRequest = new ConcurrentDictionary<string, DateTime>();
        }

        /// <summary>
        /// 計算並傳回「重試後」的秒數。
        /// </summary>
        /// <param name="clientId">用戶端的唯一識別碼。</param>
        /// <param name="currentLoad">目前的系統負載。</param>
        /// <returns>建議的重試延遲秒數。</returns>
        public int CalculateRetryAfter(string clientId, double currentLoad = 0.5)
        {
            var baseRetryAfter = _configuration.GetValue<int>("RateLimit:BaseRetryAfterSeconds", 1);
            var maxRetryAfter = _configuration.GetValue<int>("RateLimit:MaxRetryAfterSeconds", 60);
            
            // 取得此用戶端的目前重試次數
            var retryCount = _clientRetryCount.GetOrAdd(clientId, 0);
            
            // 計算指數退避：base * 2^retryCount
            var exponentialDelay = baseRetryAfter * Math.Pow(2, Math.Min(retryCount, 6)); // 上限為 2^6 = 64
            
            // 應用基於負載的乘數 (較高的負載 = 較長的延遲)
            var loadMultiplier = 1.0 + currentLoad;
            
            // 新增抖動以防止「驚群效應」(±20% 的隨機性)
            var random = new Random();
            var jitter = 0.8 + (random.NextDouble() * 0.4); // 0.8 到 1.2
            
            var finalDelay = (int)(exponentialDelay * loadMultiplier * jitter);
            
            // 確保不超過最大值
            finalDelay = Math.Min(finalDelay, maxRetryAfter);
            
            // 更新用戶端重試次數
            _clientRetryCount[clientId] = retryCount + 1;
            _clientLastRequest[clientId] = DateTime.UtcNow;
            
            _logger.LogInformation("用戶端 {ClientId} 在 {RetryAfter} 秒後重試 (嘗試次數 {RetryCount}, 負載 {Load:F2})", 
                clientId, finalDelay, retryCount + 1, currentLoad);
            
            return finalDelay;
        }

        /// <summary>
        /// 重設指定用戶端的重試次數。
        /// </summary>
        /// <param name="clientId">用戶端的唯一識別碼。</param>
        public void ResetClientRetryCount(string clientId)
        {
            _clientRetryCount.TryRemove(clientId, out _);
            _clientLastRequest.TryRemove(clientId, out _);
            _logger.LogInformation("用戶端 {ClientId} 的重試次數已重設", clientId);
        }

        /// <summary>
        /// 取得目前的系統負載。
        /// </summary>
        /// <returns>表示目前系統負載的浮點數 (0.0 至 1.0)。</returns>
        public double GetCurrentSystemLoad()
        {
            // 根據最近的請求頻率進行簡單的負載計算
            var recentRequests = _clientLastRequest.Values
                .Count(lastRequest => DateTime.UtcNow - lastRequest < TimeSpan.FromMinutes(1));
            
            var maxRequestsPerMinute = _configuration.GetValue<int>("RateLimit:MaxRequestsPerMinute", 60);
            
            return Math.Min(1.0, (double)recentRequests / maxRequestsPerMinute);
        }

        /// <summary>
        /// 清理舊的用戶端項目。
        /// </summary>
        public void CleanupOldEntries()
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-10);
            
            var oldClients = _clientLastRequest
                .Where(kvp => kvp.Value < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var clientId in oldClients)
            {
                _clientRetryCount.TryRemove(clientId, out _);
                _clientLastRequest.TryRemove(clientId, out _);
            }
            
            if (oldClients.Count > 0)
            {
                _logger.LogInformation("已清理 {Count} 個舊的用戶端項目", oldClients.Count);
            }
        }
    }
}

