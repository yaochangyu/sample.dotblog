using System.Text;
using Lab.RateLimitQueueAPI.Models;
using Lab.RateLimitQueueAPI.Services;

namespace Lab.RateLimitQueueAPI.Middleware
{
    /// <summary>
    /// 處理佇列邏輯的中介軟體。
    /// </summary>
    public class QueueMiddleware
    {
        /// <summary>
        /// 要求委派，代表管線中的下一個中介軟體。
        /// </summary>
        private readonly RequestDelegate _next;
        
        /// <summary>
        /// 用於記錄日誌的記錄器。
        /// </summary>
        private readonly ILogger<QueueMiddleware> _logger;

        /// <summary>
        /// 初始化 QueueMiddleware 的新執行個體。
        /// </summary>
        /// <param name="next">下一個要求委派。</param>
        /// <param name="logger">記錄器。</param>
        public QueueMiddleware(RequestDelegate next, ILogger<QueueMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// 處理 HTTP 要求。
        /// </summary>
        /// <param name="context">HTTP 上下文。</param>
        /// <param name="queueHandler">佇列處理常式。</param>
        public async Task InvokeAsync(HttpContext context, IQueueHandler queueHandler)
        {
            // 檢查此端點是否為應使用佇列的速率限制端點
            var endpoint = context.GetEndpoint();
            var rateLimitAttribute = endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute>();
            
            if (rateLimitAttribute != null && ShouldUseQueue(rateLimitAttribute.PolicyName))
            {
                // 儲存原始回應串流
                var originalBodyStream = context.Response.Body;
                
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                try
                {
                    await _next(context);
                    
                    // 檢查回應是否為 429 (請求過多)
                    if (context.Response.StatusCode == 429)
                    {
                        await HandleRateLimitExceeded(context, queueHandler, originalBodyStream);
                        return;
                    }
                }
                finally
                {
                    // 將回應複製回原始串流
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                    context.Response.Body = originalBodyStream;
                }
            }
            else
            {
                await _next(context);
            }
        }

        /// <summary>
        /// 判斷是否應使用佇列。
        /// </summary>
        /// <param name="policyName">速率限制策略的名稱。</param>
        /// <returns>如果應使用佇列則為 true，否則為 false。</returns>
        private bool ShouldUseQueue(string? policyName)
        {
            // 僅對 FIFO 和 Priority 策略使用佇列，不對 Adaptive 使用
            return policyName == "FifoPolicy" || policyName == "PriorityPolicy";
        }

        /// <summary>
        /// 處理超過速率限制的情況。
        /// </summary>
        /// <param name="context">HTTP 上下文。</param>
        /// <param name="queueHandler">佇列處理常式。</param>
        /// <param name="originalBodyStream">原始回應主體串流。</param>
        private async Task HandleRateLimitExceeded(HttpContext context, IQueueHandler queueHandler, Stream originalBodyStream)
        {
            try
            {
                // 檢查佇列是否已滿
                if (await queueHandler.IsQueueFullAsync())
                {
                    // 傳回 429 並附上佇列已滿的訊息
                    context.Response.StatusCode = 429;
                    context.Response.Body = originalBodyStream;
                    
                    var queueFullResponse = new
                    {
                        Error = "超過速率限制且佇列已滿",
                        Message = "請稍後再試",
                        Timestamp = DateTime.UtcNow
                    };
                    
                    await context.Response.WriteAsJsonAsync(queueFullResponse);
                    return;
                }

                // 建立佇列項目
                var queueItem = await CreateQueueItem(context);
                
                // 將要求加入佇列
                var queueId = await queueHandler.EnqueueAsync(queueItem);
                
                // 傳回 202 Accepted 並附上佇列資訊
                context.Response.StatusCode = 202;
                context.Response.Body = originalBodyStream;
                
                var queueResponse = new
                {
                    Message = "要求已排入佇列等待處理",
                    QueueId = queueId,
                    Status = "Queued",
                    EstimatedWaitTimeSeconds = await CalculateEstimatedWaitTime(queueHandler, queueItem.Priority),
                    StatusUrl = $"/api/queue/{queueId}/status",
                    Timestamp = DateTime.UtcNow
                };
                
                context.Response.Headers["Retry-After"] = "5";
                await context.Response.WriteAsJsonAsync(queueResponse);
                
                _logger.LogInformation("要求已排入佇列，ID 為 {QueueId}，路徑為 {Path}", queueId, context.Request.Path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理超過速率限制時發生錯誤");
                
                // 退回至原始的 429 回應
                context.Response.StatusCode = 429;
                context.Response.Body = originalBodyStream;
                
                var errorResponse = new
                {
                    Error = "超過速率限制",
                    Message = "無法將要求排入佇列，請稍後再試",
                    Timestamp = DateTime.UtcNow
                };
                
                await context.Response.WriteAsJsonAsync(errorResponse);
            }
        }

        /// <summary>
        /// 從 HTTP 上下文建立佇列項目。
        /// </summary>
        /// <param name="context">HTTP 上下文。</param>
        /// <returns>建立的佇列項目。</returns>
        private async Task<QueueItem> CreateQueueItem(HttpContext context)
        {
            var queueItem = new QueueItem
            {
                RequestPath = context.Request.Path,
                HttpMethod = context.Request.Method
            };

            // 複製標頭
            foreach (var header in context.Request.Headers)
            {
                queueItem.Headers[header.Key] = string.Join(", ", header.Value.ToArray());
            }

            // 如果存在，則複製主體
            if (context.Request.ContentLength > 0)
            {
                context.Request.EnableBuffering();
                context.Request.Body.Position = 0;
                
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                queueItem.Body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            // 從查詢參數或標頭中擷取優先權
            if (context.Request.Query.TryGetValue("priority", out var priorityValue))
            {
                if (int.TryParse(priorityValue, out var priority))
                {
                    queueItem.Priority = Math.Max(0, Math.Min(10, priority)); // 將值限制在 0-10 之間
                }
            }

            return queueItem;
        }

        /// <summary>
        /// 計算預估的等待時間。
        /// </summary>
        /// <param name="queueHandler">佇列處理常式。</param>
        /// <param name="priority">要求的優先權。</param>
        /// <returns>預估的等待時間 (秒)。</returns>
        private async Task<int> CalculateEstimatedWaitTime(IQueueHandler queueHandler, int priority)
        {
            var queueLength = await queueHandler.GetQueueLengthAsync();
            
            // 基本計算：佇列中的每個項目 1 秒
            var baseWaitTime = queueLength * 1;
            
            // 根據優先權進行調整 (較高的優先權 = 較短的等待時間)
            var priorityMultiplier = Math.Max(0.1, 1.0 - (priority * 0.1));
            
            return (int)(baseWaitTime * priorityMultiplier);
        }
    }
}

