using Lab.QueueApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Lab.QueueApi.RateLimit;

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
    private readonly ICommandQueueProvider _commandQueue;

    /// <summary>
    /// 記錄器。
    /// </summary>
    private readonly ILogger<CommandController> _logger;

    /// <summary>
    /// 初始化 QueueController 的新執行個體。
    /// </summary>
    /// <param name="rateLimiter">速率限制器服務。</param>
    /// <param name="commandQueue">請求佇列提供者。</param>
    /// <param name="logger">記錄器。</param>
    public CommandController(
        IRateLimiter rateLimiter,
        ICommandQueueProvider commandQueue,
        ILogger<CommandController> logger)
    {
        _rateLimiter = rateLimiter;
        _commandQueue = commandQueue;
        _logger = logger;
    }

    /// <summary>
    /// 處理 API 請求，支援限流和排隊機制。
    /// </summary>
    /// <param name="request">API 請求資料。</param>
    /// <param name="cancel"></param>
    /// <returns>表示非同步操作結果的 IActionResult。</returns>
    [HttpPost("commands")]
    public async Task<IActionResult> CreateCommandAsync([FromBody] CreateCommandRequest request,
        CancellationToken cancel = default)
    {
        try
        {
            // 前置條件檢查
            var isQueueFull = _commandQueue.IsQueueFull();
            if (isQueueFull)
            {
                // 加入佇列
                var requestId = await _commandQueue.EnqueueCommandAsync(request.Data, cancel);
                var retryAfter = _rateLimiter.GetRetryAfter();
                _logger.LogWarning("Queue is full, request {RequestId} queued", requestId);
                return StatusCode(429, new
                {
                    Message = "Too many requests. Queue is full, please retry after the specified time.",
                    RequestId = requestId,
                    RetryAfterSeconds = (int)retryAfter.TotalSeconds,
                    QueueLength = _commandQueue.GetQueueLength(),
                    MaxCapacity = _commandQueue.GetMaxCapacity(),
                    Reason = "QueueFull"
                });
            }

            var isRateLimitExceeded = !_rateLimiter.IsAllowed();

            // 任一條件成立，都要加入佇列並回傳 429
            if (isRateLimitExceeded)
            {
                // 加入佇列
                var requestId = await _commandQueue.EnqueueCommandAsync(request.Data, cancel);
                var retryAfter = _rateLimiter.GetRetryAfter();

                // 設定回應標頭
                Response.Headers["Retry-After"] = ((int)retryAfter.TotalSeconds).ToString();
                Response.Headers["X-Queue-Position"] = _commandQueue.GetQueueLength().ToString();
                Response.Headers["X-Request-Id"] = requestId;

                _logger.LogWarning("Rate limit exceeded and queue is full, request {RequestId} queued", requestId);
                return StatusCode(429, new
                {
                    Message =
                        "Too many requests. Rate limit exceeded and queue is full, please retry after the specified time.",
                    RequestId = requestId,
                    RetryAfterSeconds = (int)retryAfter.TotalSeconds,
                    QueueLength = _commandQueue.GetQueueLength(),
                    MaxCapacity = _commandQueue.GetMaxCapacity(),
                    Reason = "Rate Limit Exceeded or Queue Full."
                });
            }

            _rateLimiter.RecordRequest();
            var response = await ExecuteBusinessLogicAsync(request, cancel);

            _logger.LogInformation("Request processed directly");
            return Ok(response);
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
    /// <param name="cancel"></param>
    /// <returns>表示非同步操作結果的 IActionResult。</returns>
    [HttpGet("commands/{requestId}/status")]
    public async Task<IActionResult> GetRequestStatusAsync(string requestId, CancellationToken cancel = default)
    {
        try
        {
            // 先快速檢查是否已完成
            var response = await _commandQueue.WaitForResponseAsync(requestId, TimeSpan.FromMilliseconds(100));

            if (response.Success)
            {
                return Ok(response);
            }

            if (response.Message == "Request not found")
            {
                return NotFound(new { Message = response.Message });
            }

            // 如果還沒完成，取得所有排隊請求來確定狀態
            var allRequests = await _commandQueue.GetAllQueuedCommandsAsync(cancel);
            var currentRequest = allRequests.FirstOrDefault(r => r.Id == requestId);

            if (currentRequest == null)
            {
                return NotFound(new { Message = "Request not found" });
            }

            return Ok(new
            {
                RequestId = requestId,
                Status = currentRequest.Status.ToString(),
                QueuedAt = currentRequest.QueuedAt,
                StatusUpdatedAt = currentRequest.StatusUpdatedAt,
                Message = GetStatusMessage(currentRequest.Status),
                CanExecute = currentRequest.Status == QueuedCommandStatus.Ready
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking request status for {RequestId}", requestId);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// 根據狀態取得對應的訊息。
    /// </summary>
    /// <param name="status">請求狀態。</param>
    /// <returns>狀態描述訊息。</returns>
    private static string GetStatusMessage(QueuedCommandStatus status)
    {
        return status switch
        {
            QueuedCommandStatus.Queued => "Request is waiting in queue",
            QueuedCommandStatus.Ready => "Request is ready for execution",
            QueuedCommandStatus.Processing => "Request is being processed",
            QueuedCommandStatus.Failed => "Request has failed",
            QueuedCommandStatus.Finished => "Request has been finished and removed from queue",
            _ => "Unknown status"
        };
    }

    /// <summary>
    /// 等待排隊請求完成（用於客戶端重試）。
    /// </summary>
    /// <param name="requestId">請求的唯一識別碼。</param>
    /// <param name="cancel"></param>
    /// <returns>表示非同步操作結果的 IActionResult。</returns>
    [HttpGet("commands/{requestId}/wait")]
    public async Task<IActionResult> WaitForCommandAsync(string requestId, CancellationToken cancel = default)
    {
        try
        {
            // 嘗試執行準備好的請求
            var queuedRequest = await _commandQueue.ExecuteReadyRequestAsync(requestId, cancel);
            if (queuedRequest == null)
            {
                return NotFound(new { Message = "Request not found" });
            }

            if (queuedRequest.Status == QueuedCommandStatus.Processing)
            {
                _logger.LogInformation("Executing business logic for request {RequestId}", requestId);

                // 執行務邏輯
                var response = await ExecuteBusinessLogicAsync(queuedRequest, cancel);

                // 將請求標記為 Finished 並從佇列中移除
                await _commandQueue.FinishAndRemoveRequestAsync(queuedRequest, response, cancel);

                _logger.LogInformation("Request {RequestId} executed, finished and removed from queue", requestId);
                return Ok(response);
            }

            _logger.LogWarning("Request {RequestId} is not ready for execution", requestId);
            return StatusCode(429, new
            {
                Message = "Request is not ready for execution",
                RequestId = requestId,
                QueuePosition = _commandQueue.GetQueueLength()
            });
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
            QueueLength = _commandQueue.GetQueueLength(),
            CanAcceptRequest = _rateLimiter.IsAllowed(),
            RetryAfterSeconds = (int)_rateLimiter.GetRetryAfter().TotalSeconds,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 列出佇列中所有的請求。
    /// </summary>
    /// <returns>包含所有佇列任務資訊的 IActionResult。</returns>
    [HttpGet("commands")]
    public async Task<IActionResult> GetAllCommandsAsync(CancellationToken cancel = default)
    {
        try
        {
            var currentTime = DateTime.UtcNow;
            var queuedRequests = await _commandQueue.GetAllQueuedCommandsAsync(cancel);

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
    /// 取得清理記錄摘要。
    /// </summary>
    /// <returns>包含清理記錄資訊的 IActionResult。</returns>
    [HttpGet("commands/cleanup-summary")]
    public async Task<IActionResult> GetCleanupSummaryAsync(CancellationToken cancel = default)
    {
        try
        {
            var cleanupSummary = await _commandQueue.GetCleanupSummaryAsync(cancel);

            _logger.LogInformation("Retrieved cleanup summary with {TotalCount} records",
                cleanupSummary.TotalCleanupCount);

            return Ok(cleanupSummary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cleanup summary");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// 執行實際的業務邏輯
    /// </summary>
    /// <param name="request">要處理的 API 請求。</param>
    /// <param name="cancel"></param>
    /// <returns>表示非同步操作的 Task，其結果為 ApiResponse。</returns>
    private async Task<QueuedCommandResponse> ExecuteBusinessLogicAsync(CreateCommandRequest request,
        CancellationToken cancel = default)
    {
        // 模擬處理時間
        await Task.Delay(TimeSpan.FromSeconds(1), cancel);

        return new QueuedCommandResponse
        {
            Success = true,
            Message = "Request processed successfully (direct)",
            Data = new
            {
                OriginalData = request.Data,
                ProcessedData = request.Data,
                ProcessingType = "Direct"
            }
        };
    }

    /// <summary>
    /// 執行實際的業務邏輯（從佇列處理的請求）。
    /// </summary>
    /// <param name="queuedRequest">排隊的請求上下文。</param>
    /// <param name="cancel">取消令牌。</param>
    /// <returns>表示非同步操作的 Task，其結果為 QueuedCommandResponse。</returns>
    private async Task<QueuedCommandResponse> ExecuteBusinessLogicAsync(QueuedContext queuedRequest,
        CancellationToken cancel = default)
    {
        // 模擬業務邏輯處理時間
        await Task.Delay(TimeSpan.FromSeconds(1), cancel);

        return new QueuedCommandResponse
        {
            Success = true,
            Message = "Request processed successfully (from queue)",
            Data = new
            {
                RequestId = queuedRequest.Id,
                OriginalData = queuedRequest.RequestData,
                QueuedAt = queuedRequest.QueuedAt,
                ProcessedAt = DateTime.UtcNow,
                ProcessingType = "Queued",
                Status = queuedRequest.Status.ToString()
                // Status = QueuedCommandStatus.Finished
            }
        };
    }
}