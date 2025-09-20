using Lab.QueueApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lab.QueueApi.Commands;

/// <summary>
/// API 控制器，用於處理具有速率限制和佇列功能的請求。
/// </summary>
[ApiController]
[Route("api")]
public class CommandController : ControllerBase
{
    /// <summary>
    /// 速率限制器服務。
    /// </summary>
    private readonly IRateLimiter _rateLimiter;

    /// <summary>
    /// 請求佇列提供者。
    /// </summary>
    private readonly IRequestQueueProvider _requestQueue;

    /// <summary>
    /// 記錄器。
    /// </summary>
    private readonly ILogger<CommandController> _logger;

    /// <summary>
    /// 初始化 QueueController 的新執行個體。
    /// </summary>
    /// <param name="rateLimiter">速率限制器服務。</param>
    /// <param name="requestQueue">請求佇列提供者。</param>
    /// <param name="logger">記錄器。</param>
    public CommandController(
        IRateLimiter rateLimiter,
        IRequestQueueProvider requestQueue,
        ILogger<CommandController> logger)
    {
        _rateLimiter = rateLimiter;
        _requestQueue = requestQueue;
        _logger = logger;
    }

    /// <summary>
    /// 處理 API 請求，支援限流和排隊機制。
    /// </summary>
    /// <param name="request">API 請求資料。</param>
    /// <returns>表示非同步操作結果的 IActionResult。</returns>
    [HttpPost("commands")]
    public async Task<IActionResult> Create([FromBody] CreateCommandRequest request)
    {
        try
        {
            // 檢查限流
            if (_rateLimiter.IsRequestAllowed())
            {
                // 直接處理請求
                _rateLimiter.RecordRequest();
                var response = await ProcessDirectlyAsync(request);

                _logger.LogInformation("Request processed directly");
                return Ok(response);
            }

            // 請求進入佇列
            var requestId = await _requestQueue.EnqueueCommandAsync(request.Data);
            var retryAfter = _rateLimiter.GetRetryAfter();

            _logger.LogInformation("Request {RequestId} queued, retry after {RetryAfter}s",
                requestId, retryAfter.TotalSeconds);

            // 返回 429 狀態碼和 Retry-After 標頭
            Response.Headers["Retry-After"] = ((int)retryAfter.TotalSeconds).ToString();
            Response.Headers["X-Queue-Position"] = _requestQueue.GetQueueLength().ToString();
            Response.Headers["X-Request-Id"] = requestId;

            return StatusCode(429, new
            {
                Message = "Too many requests. Please retry after the specified time.",
                RequestId = requestId,
                RetryAfterSeconds = (int)retryAfter.TotalSeconds,
                QueuePosition = _requestQueue.GetQueueLength()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// 檢查排隊請求的狀態。
    /// </summary>
    /// <param name="requestId">請求的唯一識別碼。</param>
    /// <returns>表示非同步操作結果的 IActionResult。</returns>
    [HttpGet("commadns/{requestId}/status")]
    public async Task<IActionResult> GetRequestStatus(string requestId)
    {
        try
        {
            // 等待請求完成，設定較短的超時時間用於狀態檢查
            var response = await _requestQueue.WaitForResponseAsync(requestId, TimeSpan.FromSeconds(1));

            if (response.Success)
            {
                return Ok(response);
            }

            if (response.Message == "Request timeout")
            {
                return Accepted(new
                {
                    Message = "Request is still being processed",
                    RequestId = requestId,
                    QueuePosition = _requestQueue.GetQueueLength()
                });
            }

            return NotFound(new { Message = response.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking request status for {RequestId}", requestId);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// 等待排隊請求完成（用於客戶端重試）。
    /// </summary>
    /// <param name="requestId">請求的唯一識別碼。</param>
    /// <returns>表示非同步操作結果的 IActionResult。</returns>
    [HttpGet("commands/{requestId}/wait")]
    public async Task<IActionResult> WaitForRequest(string requestId)
    {
        try
        {
            // 等待請求完成，設定較長的超時時間
            var response = await _requestQueue.WaitForResponseAsync(requestId, TimeSpan.FromMinutes(2));

            if (response.Success)
            {
                return Ok(response);
            }

            if (response.Message == "Request timeout")
            {
                return StatusCode(408, new
                {
                    Message = "Request processing timeout",
                    RequestId = requestId
                });
            }

            return NotFound(new { Message = response.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error waiting for request {RequestId}", requestId);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// 獲取系統的健康狀態。
    /// </summary>
    /// <returns>包含系統健康狀態資訊的 IActionResult。</returns>
    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            Status = "Healthy",
            QueueLength = _requestQueue.GetQueueLength(),
            CanAcceptRequest = _rateLimiter.IsRequestAllowed(),
            RetryAfterSeconds = (int)_rateLimiter.GetRetryAfter().TotalSeconds,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 列出佇列中所有的請求。
    /// </summary>
    /// <returns>包含所有佇列任務資訊的 IActionResult。</returns>
    [HttpGet("commands")]
    public IActionResult GetAllCommand()
    {
        try
        {
            var currentTime = DateTime.UtcNow;
            var queuedRequests = _requestQueue.GetAllQueuedCommands();

            var commands = queuedRequests.Select(req => new GetCommandStatusResponse
            {
                Id = req.Id,
                QueuedAt = req.QueuedAt,
                RequestData = req.RequestData,
                WaitingTime = currentTime - req.QueuedAt
            }).ToList();

            _logger.LogInformation("Retrieved {TaskCount} queued tasks", commands.Count);

            return Ok(new
            {
                TotalCommandCount = commands.Count,
                Commands = commands,
                Timestamp = currentTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving queued tasks");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// 非同步地直接處理請求。
    /// </summary>
    /// <param name="request">要處理的 API 請求。</param>
    /// <returns>表示非同步操作的 Task，其結果為 ApiResponse。</returns>
    private async Task<QueuedCommandResponse> ProcessDirectlyAsync(CreateCommandRequest request)
    {
        // 模擬處理時間
        await Task.Delay(TimeSpan.FromSeconds(1));

        return new QueuedCommandResponse
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