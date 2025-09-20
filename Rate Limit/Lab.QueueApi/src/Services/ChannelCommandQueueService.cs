using Lab.QueueApi.Commands;

namespace Lab.QueueApi.Services;

/// <summary>
/// 一個背景服務，用於處理佇列中的請求。
/// </summary>
public class ChannelCommandQueueService : BackgroundService
{
    /// <summary>
    /// 請求佇列的提供者。
    /// </summary>
    private readonly ICommandQueueProvider _commandQueue;

    /// <summary>
    /// 速率限制器。
    /// </summary>
    private readonly IRateLimiter _rateLimiter;

    /// <summary>
    /// 記錄器。
    /// </summary>
    private readonly ILogger<ChannelCommandQueueService> _logger;

    /// <summary>
    /// 初始化 ChannelRequestQueueService 的新執行個體。
    /// </summary>
    /// <param name="commandQueue">請求佇列的提供者。</param>
    /// <param name="rateLimiter">速率限制器。</param>
    /// <param name="logger">記錄器。</param>
    public ChannelCommandQueueService(
        ICommandQueueProvider commandQueue,
        IRateLimiter rateLimiter,
        ILogger<ChannelCommandQueueService> logger)
    {
        _commandQueue = commandQueue;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    /// <summary>
    /// 執行背景服務的主要邏輯。
    /// </summary>
    /// <param name="stoppingToken">用於觸發服務停止的 CancellationToken。</param>
    /// <returns>表示非同步操作的 Task。</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Request Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var queuedRequest = await _commandQueue.DequeueCommandAsync(stoppingToken);

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

                await _commandQueue.CompleteCommandAsync(queuedRequest.Id, response, stoppingToken);

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

    /// <summary>
    /// 非同步地處理一個已排入佇列的請求。
    /// </summary>
    /// <param name="queuedRequest">要處理的已排入佇列的請求。</param>
    /// <returns>表示非同步操作的 Task，其結果為 ApiResponse。</returns>
    private async Task<QueuedCommandResponse> ProcessRequestAsync(QueuedContext queuedRequest)
    {
        // 模擬處理時間
        await Task.Delay(TimeSpan.FromSeconds(1));

        return new QueuedCommandResponse
        {
            Success = true,
            Message = "Request processed successfully",
            Data = new
            {
                RequestId = queuedRequest.Id,
                OriginalData = queuedRequest.RequestData,
                QueuedAt = queuedRequest.QueuedAt,
                ProcessedData = queuedRequest.RequestData
            }
        };
    }
}