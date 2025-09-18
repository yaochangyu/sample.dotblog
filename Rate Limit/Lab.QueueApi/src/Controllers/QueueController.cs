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
    private readonly ILogger<QueueController> _logger;

    public QueueController(
        IRateLimiter rateLimiter,
        IQueueHandler queueHandler,
        ILogger<QueueController> logger)
    {
        _rateLimiter = rateLimiter;
        _queueHandler = queueHandler;
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
            // 使用整合的等待和處理方法
            var response = await WaitForResponseWithProcessingAsync(requestId, TimeSpan.FromSeconds(10), cancel);

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
    /// 等待請求回應並主動處理佇列（整合版本）
    /// </summary>
    private async Task<ApiResponse> WaitForResponseWithProcessingAsync(string requestId, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var endTime = startTime.Add(timeout);

        while (DateTime.UtcNow < endTime)
        {
            // 先檢查請求是否已經完成
            var quickCheck = await _queueHandler.WaitForResponseAsync(requestId, TimeSpan.FromMilliseconds(100), cancellationToken);
            if (quickCheck.Success)
            {
                return quickCheck;
            }

            // 如果請求不存在，直接返回
            if (quickCheck.Message == "Request not found")
            {
                return quickCheck;
            }

            // 嘗試處理一個佇列請求
            var processed = await _queueHandler.TryProcessNextRequestAsync(cancellationToken);
            if (processed)
            {
                _logger.LogDebug("Processed a queued request while waiting");
                continue; // 處理了請求，立即檢查目標請求是否完成
            }

            // 沒有處理任何請求，檢查是否因為限流
            if (!_rateLimiter.IsRequestAllowed() && _queueHandler.GetQueueLength() > 0)
            {
                // 限流中，等待一段時間
                var retryAfter = _rateLimiter.GetRetryAfter();
                var waitTime = TimeSpan.FromMilliseconds(Math.Min(retryAfter.TotalMilliseconds + 100, 1000));
                await Task.Delay(waitTime, cancellationToken);
            }
            else
            {
                // 佇列為空或其他原因，短暫等待
                await Task.Delay(200, cancellationToken);
            }
        }

        // 超時
        return new ApiResponse
        {
            Success = false,
            Message = "Request timeout"
        };
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
