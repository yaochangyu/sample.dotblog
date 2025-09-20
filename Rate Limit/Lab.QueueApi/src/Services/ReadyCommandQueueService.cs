using Lab.QueueApi.Commands;
using Lab.QueueApi.RateLimit;

namespace Lab.QueueApi.Services;

/// <summary>
/// 一個背景服務，用於處理佇列中的請求。
/// </summary>
public class ReadyCommandQueueService : BackgroundService
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
    private readonly ILogger<ReadyCommandQueueService> _logger;

    /// <summary>
    /// 處理請求時的模擬延遲時間。
    /// </summary>
    private readonly TimeSpan _processingDelay;

    /// <summary>
    /// 佇列空時的等待間隔。
    /// </summary>
    private readonly TimeSpan _emptyQueueDelay;

    /// <summary>
    /// 發生錯誤時的重試延遲。
    /// </summary>
    private readonly TimeSpan _errorRetryDelay;

    /// <summary>
    /// 初始化 ChannelRequestQueueService 的新執行個體。
    /// </summary>
    /// <param name="commandQueue">請求佇列的提供者。</param>
    /// <param name="rateLimiter">速率限制器。</param>
    /// <param name="logger">記錄器。</param>
    /// <param name="processingDelay">處理請求時的模擬延遲時間。</param>
    /// <param name="emptyQueueDelay">佇列空時的等待間隔。</param>
    /// <param name="errorRetryDelay">發生錯誤時的重試延遲。</param>
    public ReadyCommandQueueService(
        ICommandQueueProvider commandQueue,
        IRateLimiter rateLimiter,
        ILogger<ReadyCommandQueueService> logger,
        TimeSpan processingDelay,
        TimeSpan emptyQueueDelay,
        TimeSpan errorRetryDelay)
    {
        _commandQueue = commandQueue;
        _rateLimiter = rateLimiter;
        _logger = logger;
        _processingDelay = processingDelay;
        _emptyQueueDelay = emptyQueueDelay;
        _errorRetryDelay = errorRetryDelay;
    }

    /// <summary>
    /// 執行背景服務的主要邏輯。
    /// </summary>
    /// <param name="stoppingToken">用於觸發服務停止的 CancellationToken。</param>
    /// <returns>表示非同步操作的 Task。</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Permission Manager started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var queuedRequest = await _commandQueue.GetNextQueuedRequestAsync(stoppingToken);

                if (queuedRequest == null)
                {
                    await Task.Delay(_emptyQueueDelay, stoppingToken);
                    continue;
                }

                // 等待直到可以許可請求（遵守限流規則）
                while (!_rateLimiter.IsAllowed())
                {
                    var retryAfter = _rateLimiter.GetRetryAfter();
                    _logger.LogDebug("Rate limit reached, waiting {RetryAfter}ms", retryAfter.TotalMilliseconds);
                    await Task.Delay(retryAfter.Add(TimeSpan.FromMilliseconds(100)), stoppingToken);
                }

                // 只記錄許可並更新狀態為 Ready，不執行實際業務邏輯
                _rateLimiter.RecordRequest();
                await _commandQueue.MarkRequestAsReadyAsync(queuedRequest.Id, stoppingToken);

                _logger.LogInformation("Request {RequestId} is now ready for processing", queuedRequest.Id);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error managing request permissions");
                await Task.Delay(_errorRetryDelay, stoppingToken);
            }
        }

        _logger.LogInformation("Background Permission Manager stopped");
    }

}