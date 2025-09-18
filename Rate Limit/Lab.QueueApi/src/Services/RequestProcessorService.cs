using Lab.QueueApi.Models;

namespace Lab.QueueApi.Services;

public class RequestProcessorService
{
    private readonly IQueueHandler _queueHandler;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<RequestProcessorService> _logger;

    public RequestProcessorService(
        IQueueHandler queueHandler,
        IRateLimiter rateLimiter,
        ILogger<RequestProcessorService> logger)
    {
        _queueHandler = queueHandler;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task<bool> TryProcessNextRequestAsync()
    {
        try
        {
            // 檢查是否可以處理請求
            if (!_rateLimiter.IsRequestAllowed())
            {
                return false;
            }

            // 嘗試從佇列取出請求
            var queuedRequest = await _queueHandler.DequeueRequestAsync(CancellationToken.None);

            if (queuedRequest == null)
            {
                return false;
            }

            // 記錄請求並處理
            _rateLimiter.RecordRequest();
            var response = await ProcessRequestAsync(queuedRequest);

            _queueHandler.CompleteRequest(queuedRequest.Id, response);

            _logger.LogInformation("Processed request {RequestId} on-demand", queuedRequest.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing queued request on-demand");
            return false;
        }
    }

    public async Task<ApiResponse> ProcessRequestAsync(QueuedRequest queuedRequest)
    {
        // 模擬處理時間
        await Task.Delay(TimeSpan.FromSeconds(1));

        return new ApiResponse
        {
            Success = true,
            Message = "Request processed successfully",
            Data = new
            {
                RequestId = queuedRequest.Id,
                OriginalData = queuedRequest.RequestData,
                QueuedAt = queuedRequest.QueuedAt,
                ProcessedData = $"Processed: {queuedRequest.RequestData}"
            }
        };
    }
}
