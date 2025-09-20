using Lab.QueueApi.RateLimit;

namespace Lab.QueueApi.Services;

/// <summary>
/// 背景服務，定期清理超過指定時間且未被取得結果的過期請求。
/// </summary>
public class ExpiredRequestCleanupService : BackgroundService
{
    private readonly ICommandQueueProvider _queueProvider;
    private readonly ILogger<ExpiredRequestCleanupService> _logger;
    private readonly TimeSpan _maxRequestAge;
    private readonly TimeSpan _cleanupInterval;

    /// <summary>
    /// 初始化 ExpiredRequestCleanupService 的新執行個體。
    /// </summary>
    /// <param name="queueProvider">佇列提供者。</param>
    /// <param name="logger">日誌記錄器。</param>
    /// <param name="maxRequestAge">請求最大存活時間。</param>
    /// <param name="cleanupInterval">清理間隔時間。</param>
    public ExpiredRequestCleanupService(
        ICommandQueueProvider queueProvider,
        ILogger<ExpiredRequestCleanupService> logger,
        TimeSpan maxRequestAge,
        TimeSpan cleanupInterval)
    {
        _queueProvider = queueProvider ?? throw new ArgumentNullException(nameof(queueProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxRequestAge = maxRequestAge;
        _cleanupInterval = cleanupInterval;

        _logger.LogInformation(
            "ExpiredRequestCleanupService initialized with MaxRequestAge: {MaxRequestAge}, CleanupInterval: {CleanupInterval}",
            _maxRequestAge,
            _cleanupInterval);
    }

    /// <summary>
    /// 執行背景清理任務。
    /// </summary>
    /// <param name="stoppingToken">用於停止服務的取消權杖。</param>
    /// <returns>表示非同步操作的 Task。</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExpiredRequestCleanupService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);

                var cleanedCount = _queueProvider.CleanupExpiredRequests(_maxRequestAge);

                if (cleanedCount > 0)
                {
                    _logger.LogInformation(
                        "Cleaned up {CleanedCount} expired requests older than {MaxRequestAge}",
                        cleanedCount,
                        _maxRequestAge);
                }
                else
                {
                    _logger.LogDebug("No expired requests found during cleanup cycle");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("ExpiredRequestCleanupService is stopping due to cancellation request");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during expired request cleanup");
                // 發生錯誤時繼續執行，避免整個服務停止
            }
        }

        _logger.LogInformation("ExpiredRequestCleanupService stopped");
    }

    /// <summary>
    /// 服務停止時的清理工作。
    /// </summary>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>表示非同步操作的 Task。</returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ExpiredRequestCleanupService is stopping");
        await base.StopAsync(cancellationToken);
    }
}