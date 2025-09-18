using Lab.QueueApi.Models;
using Lab.QueueApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lab.QueueApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueueController : ControllerBase
{
    private readonly IRateLimiter _rateLimiter;
    private readonly IQueueHandler _queueHandler;
    private readonly RequestProcessorService _requestProcessor;
    private readonly ILogger<QueueController> _logger;

    public QueueController(
        IRateLimiter rateLimiter,
        IQueueHandler queueHandler,
        RequestProcessorService requestProcessor,
        ILogger<QueueController> logger)
    {
        _rateLimiter = rateLimiter;
        _queueHandler = queueHandler;
        _requestProcessor = requestProcessor;
        _logger = logger;
    }

    /// <summary>
    /// 處理 API 請求，支援限流和排隊機制
    /// </summary>
    /// <param name="request">API 請求資料</param>
    /// <returns>API 回應</returns>
    [HttpPost("process")]
    public async Task<IActionResult> ProcessRequest([FromBody] CreateQueueRequest request, CancellationToken cancel = default)
    {
        try
        {
            // 檢查限流
            if (_rateLimiter.IsRequestAllowed())
            {
                _rateLimiter.RecordRequest();

                // 直接處理請求
                var response = await ProcessDirectlyAsync(request);

                _logger.LogInformation("Request processed directly");
                return this.Ok(response);
            }

            // 請求進入佇列
            var requestId = await _queueHandler.EnqueueRequestAsync(request.Data, cancel);
            var retryAfter = _rateLimiter.GetRetryAfter();

            _logger.LogInformation("Request {RequestId} queued, retry after {RetryAfter}s",
                requestId, retryAfter.TotalSeconds);

            // 返回 429 狀態碼和 Retry-After 標頭
            Response.Headers["Retry-After"] = ((int)retryAfter.TotalSeconds).ToString();
            Response.Headers["X-Queue-Position"] = _queueHandler.GetQueueLength().ToString();
            Response.Headers["X-Request-Id"] = requestId;

            return this.StatusCode(429, new
            {
                Message = "Too many requests. Please retry after the specified time.",
                RequestId = requestId,
                RetryAfterSeconds = (int)retryAfter.TotalSeconds,
                QueuePosition = _queueHandler.GetQueueLength()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request");
            return this.StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// 檢查排隊請求的狀態
    /// </summary>
    /// <param name="requestId">請求 ID</param>
    /// <returns>請求狀態</returns>
    [HttpGet("status/{requestId}")]
    public async Task<IActionResult> GetRequestStatus(string requestId, CancellationToken cancel = default)
    {
        try
        {
            // 等待請求完成，設定較短的超時時間用於狀態檢查
            var response = await _queueHandler.WaitForResponseAsync(requestId, TimeSpan.FromSeconds(1), cancel);

            if (response.Success)
            {
                return this.Ok(response);
            }
            
            if (response.Message == "Request timeout")
            {
                return this.Accepted(new
                {
                    Message = "Request is still being processed",
                    RequestId = requestId,
                    QueuePosition = _queueHandler.GetQueueLength()
                });
            }
            
            return this.NotFound(new { Message = response.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking request status for {RequestId}", requestId);
            return this.StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// 等待排隊請求完成（用於客戶端重試）
    /// </summary>
    /// <param name="requestId">請求 ID</param>
    /// <returns>處理結果</returns>
    [HttpGet("wait/{requestId}")]
    public async Task<IActionResult> WaitForRequest(string requestId, CancellationToken cancel = default)
    {
        try
        {
            // 先嘗試處理佇列中的請求
            await TryProcessQueuedRequests();

            // 等待請求完成，設定較長的超時時間
            var response = await _queueHandler.WaitForResponseAsync(requestId, TimeSpan.FromSeconds(10), cancel);

            if (response.Success)
            {
                return this.Ok(response);
            }
            
            if (response.Message == "Request timeout")
            {
                return this.StatusCode(408, new
                {
                    Message = "Request processing timeout",
                    RequestId = requestId
                });
            }
            
            return this.NotFound(new { Message = response.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error waiting for request {RequestId}", requestId);
            return this.StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// 嘗試處理佇列中的請求（按需處理）
    /// </summary>
    private async Task TryProcessQueuedRequests()
    {
        // 持續處理佇列中的請求，直到無法處理更多或佇列為空
        while (true)
        {
            // 嘗試處理下一個請求
            var processed = await _requestProcessor.TryProcessNextRequestAsync();

            if (processed)
            {
                _logger.LogDebug("Processed a queued request on-demand");
            }
            else
            {
                // 無法處理更多請求（限流或佇列為空）
                // 檢查是否因為限流而無法處理
                if (!_rateLimiter.IsRequestAllowed() && _queueHandler.GetQueueLength() > 0)
                {
                    // 如果限流中且佇列不為空，等待到可以處理的時間
                    var retryAfter = _rateLimiter.GetRetryAfter();
                    _logger.LogDebug("Rate limit reached, waiting {RetryAfter}ms", retryAfter.TotalMilliseconds);
                    await Task.Delay(retryAfter.Add(TimeSpan.FromMilliseconds(100)));
                    continue;
                }
                else
                {
                    // 佇列為空或其他原因，退出
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 獲取系統狀態
    /// </summary>
    /// <returns>系統狀態資訊</returns>
    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return this.Ok(new
        {
            Status = "Healthy",
            QueueLength = _queueHandler.GetQueueLength(),
            CanAcceptRequest = _rateLimiter.IsRequestAllowed(),
            RetryAfterSeconds = (int)_rateLimiter.GetRetryAfter().TotalSeconds,
            Timestamp = DateTime.UtcNow
        });
    }

    private async Task<ApiResponse> ProcessDirectlyAsync(CreateQueueRequest request)
    {
        // 模擬處理時間
        await Task.Delay(TimeSpan.FromSeconds(1));

        return new ApiResponse
        {
            Success = true,
            Message = "Request processed successfully (direct)",
            Data = new
            {
                OriginalData = request.Data,
                ProcessedData = $"Processed: {request.Data}",
                ProcessingType = "Direct"
            }
        };
    }
}
