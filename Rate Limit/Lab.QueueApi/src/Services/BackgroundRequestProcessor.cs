using Lab.QueueApi.Models;

namespace Lab.QueueApi.Services;

public class BackgroundRequestProcessor : BackgroundService
{
    private readonly IRequestQueue _requestQueue;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<BackgroundRequestProcessor> _logger;

    public BackgroundRequestProcessor(
        IRequestQueue requestQueue,
        IRateLimiter rateLimiter,
        ILogger<BackgroundRequestProcessor> logger)
    {
        _requestQueue = requestQueue;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Request Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var queuedRequest = await _requestQueue.DequeueRequestAsync(stoppingToken);
                
                if (queuedRequest == null)
                {
                    await Task.Delay(100, stoppingToken);
                    continue;
                }

                // 等待直到可以處理請求（遵守限流規則）
                while (!_rateLimiter.IsRequestAllowed())
                {
                    var retryAfter = _rateLimiter.GetRetryAfter();
                    _logger.LogDebug("Rate limit reached, waiting {RetryAfter}ms", retryAfter.TotalMilliseconds);
                    await Task.Delay(retryAfter.Add(TimeSpan.FromMilliseconds(100)), stoppingToken);
                }

                // 記錄請求並處理
                _rateLimiter.RecordRequest();
                var response = await ProcessRequestAsync(queuedRequest);
                
                _requestQueue.CompleteRequest(queuedRequest.Id, response);
                
                _logger.LogInformation("Processed request {RequestId}", queuedRequest.Id);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queued request");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("Background Request Processor stopped");
    }

    private async Task<ApiResponse> ProcessRequestAsync(QueuedRequest queuedRequest)
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

