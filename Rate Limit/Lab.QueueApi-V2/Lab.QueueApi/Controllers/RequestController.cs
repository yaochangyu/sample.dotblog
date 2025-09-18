using Microsoft.AspNetCore.Mvc;
using Lab.QueueApi.Models;
using Lab.QueueApi.Services;

namespace Lab.QueueApi.Controllers;

/// <summary>
/// 請求處理 API 控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RequestController : ControllerBase
{
    /// <summary>
    /// 速率限制服務
    /// </summary>
    private readonly IRateLimitService _rateLimitService;

    /// <summary>
    /// 請求池服務
    /// </summary>
    private readonly IRequestPool _requestPool;

    /// <summary>
    /// 請求處理服務
    /// </summary>
    private readonly IRequestProcessor _requestProcessor;

    /// <summary>
    /// 建構子
    /// </summary>
    /// <param name="rateLimitService">速率限制服務</param>
    /// <param name="requestPool">請求池服務</param>
    /// <param name="requestProcessor">請求處理服務</param>
    public RequestController(
        IRateLimitService rateLimitService,
        IRequestPool requestPool,
        IRequestProcessor requestProcessor)
    {
        _rateLimitService = rateLimitService;
        _requestPool = requestPool;
        _requestProcessor = requestProcessor;
    }

    /// <summary>
    /// 提交請求進行處理
    /// </summary>
    /// <param name="request">要處理的請求</param>
    /// <returns>處理結果或池回應</returns>
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitRequest([FromBody] ApiRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Data))
        {
            return BadRequest("Invalid request data");
        }

        // 檢查速率限制
        if (_rateLimitService.CanProcess())
        {
            // 記錄請求並立即處理
            _rateLimitService.RecordRequest();
            var result = await _requestProcessor.ProcessRequestAsync(request);

            return Ok(new
            {
                Success = true,
                Data = result,
                Timestamp = DateTime.UtcNow
            });
        }
        else
        {
            // 超過限制，加入請求池
            var requestId = _requestPool.AddRequest(request);

            return StatusCode(429, new PoolResponse
            {
                RequestId = requestId,
                RetryAfterSeconds = 60, // 建議 1 分鐘後重試
                Message = "請求已加入處理池，請於60秒後重試",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 使用 Request ID 重試處理
    /// </summary>
    /// <param name="requestId">請求 ID</param>
    /// <returns>處理結果</returns>
    [HttpGet("retry/{requestId}")]
    public async Task<IActionResult> RetryRequest(string requestId)
    {
        var pooledRequest = _requestPool.GetByRequestId(requestId);
        if (pooledRequest == null)
        {
            return NotFound("Request ID not found");
        }

        // 檢查狀態
        if (pooledRequest.Status == "Completed")
        {
            return Ok(new
            {
                Success = true,
                Message = "Request already completed",
                RequestId = requestId,
                Timestamp = DateTime.UtcNow
            });
        }

        if (pooledRequest.Status != "Pending")
        {
            return Conflict($"Request is in {pooledRequest.Status} state");
        }

        // 檢查是否可以處理
        if (_rateLimitService.CanProcess())
        {
            // 標記為處理中
            pooledRequest.Status = "Processing";

            // 記錄請求並處理
            _rateLimitService.RecordRequest();

            try
            {
                var originalRequest = System.Text.Json.JsonSerializer.Deserialize<ApiRequest>(pooledRequest.OriginalData);
                var result = await _requestProcessor.ProcessRequestAsync(originalRequest!);

                // 標記為完成並從池中移除
                pooledRequest.Status = "Completed";
                _requestPool.RemoveRequest(requestId);

                return Ok(new
                {
                    Success = true,
                    Data = result,
                    RequestId = requestId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception)
            {
                pooledRequest.Status = "Failed";
                return StatusCode(500, "Request processing failed");
            }
        }
        else
        {
            // 仍然忙碌
            return StatusCode(429, new
            {
                Message = "Still pending",
                RequestId = requestId,
                Status = pooledRequest.Status,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 查詢請求狀態
    /// </summary>
    /// <param name="requestId">請求 ID</param>
    /// <returns>請求狀態資訊</returns>
    [HttpGet("status/{requestId}")]
    public IActionResult GetRequestStatus(string requestId)
    {
        var pooledRequest = _requestPool.GetByRequestId(requestId);
        if (pooledRequest == null)
        {
            return NotFound("Request ID not found");
        }

        var response = new
        {
            RequestId = requestId,
            Status = pooledRequest.Status,
            CreatedAt = pooledRequest.CreatedTime,
            EstimatedProcessTime = pooledRequest.CreatedTime.AddMinutes(1), // 預估處理時間
            Timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// 取消指定請求
    /// </summary>
    /// <param name="requestId">請求 ID</param>
    /// <returns>取消結果</returns>
    [HttpDelete("cancel/{requestId}")]
    public IActionResult CancelRequest(string requestId)
    {
        var pooledRequest = _requestPool.GetByRequestId(requestId);
        if (pooledRequest == null)
        {
            return NotFound("Request ID not found");
        }

        if (pooledRequest.Status != "Pending")
        {
            return Conflict($"Cannot cancel request in {pooledRequest.Status} state");
        }

        // 標記為取消並移除
        pooledRequest.Status = "Canceled";
        _requestPool.RemoveRequest(requestId);

        return Ok(new
        {
            Message = "Request cancelled successfully",
            RequestId = requestId,
            Timestamp = DateTime.UtcNow
        });
    }
}