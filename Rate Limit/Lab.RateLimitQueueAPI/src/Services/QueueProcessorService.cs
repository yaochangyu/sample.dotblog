using Lab.RateLimitQueueAPI.Models;

namespace Lab.RateLimitQueueAPI.Services
{
    /// <summary>
    /// 在背景處理佇列中項目的服務。
    /// </summary>
    public class QueueProcessorService : BackgroundService
    {
        /// <summary>
        /// 服務提供者，用於建立服務範圍。
        /// </summary>
        private readonly IServiceProvider _serviceProvider;
        
        /// <summary>
        /// 用於記錄日誌的記錄器。
        /// </summary>
        private readonly ILogger<QueueProcessorService> _logger;
        
        /// <summary>
        /// 應用程式組態。
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// 初始化 QueueProcessorService 的新執行個體。
        /// </summary>
        /// <param name="serviceProvider">服務提供者。</param>
        /// <param name="logger">記錄器。</param>
        /// <param name="configuration">應用程式組態。</param>
        public QueueProcessorService(
            IServiceProvider serviceProvider,
            ILogger<QueueProcessorService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// 執行背景服務。
        /// </summary>
        /// <param name="stoppingToken">用於停止服務的取消權杖。</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var processingInterval = TimeSpan.FromMilliseconds(
                _configuration.GetValue<int>("Queue:ProcessingIntervalMs", 1000));

            _logger.LogInformation("佇列處理器已啟動，間隔為 {Interval} 毫秒", processingInterval.TotalMilliseconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var queueService = scope.ServiceProvider.GetRequiredService<IQueueHandler>();

                    // 從佇列中處理一個項目
                    var item = await queueService.DequeueAsync();
                    if (item != null)
                    {
                        await ProcessQueueItem(item, queueService);
                    }

                    // 定期清理過期的項目
                    await queueService.CleanupExpiredItemsAsync();

                    // 等待處理下一個項目
                    await Task.Delay(processingInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // 當請求取消時，這是預期的
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "佇列處理器發生錯誤");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // 重試前等待
                }
            }

            _logger.LogInformation("佇列處理器已停止");
        }

        /// <summary>
        /// 處理單一佇列項目。
        /// </summary>
        /// <param name="item">要處理的佇列項目。</param>
        /// <param name="queueHandler">佇列處理常式。</param>
        private async Task ProcessQueueItem(QueueItem item, IQueueHandler queueHandler)
        {
            try
            {
                _logger.LogInformation("正在處理佇列項目 {QueueId}", item.Id);

                // 模擬處理原始請求
                // 在實際實作中，您會：
                // 1. 重新建立原始的 HTTP 請求
                // 2. 執行業務邏輯
                // 3. 傳回結果

                var result = await SimulateRequestProcessing(item);

                await queueHandler.UpdateQueueItemStatusAsync(
                    item.Id, 
                    QueueStatus.Completed, 
                    result);

                _logger.LogInformation("佇列項目 {QueueId} 已成功處理", item.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理佇列項目 {QueueId} 失敗", item.Id);
                
                await queueHandler.UpdateQueueItemStatusAsync(
                    item.Id, 
                    QueueStatus.Failed, 
                    errorMessage: ex.Message);
            }
        }

        /// <summary>
        /// 模擬請求處理。
        /// </summary>
        /// <param name="item">要處理的佇列項目。</param>
        /// <returns>模擬的處理結果。</returns>
        private async Task<string> SimulateRequestProcessing(QueueItem item)
        {
            // 模擬一些處理時間
            await Task.Delay(100);

            // 根據請求路徑模擬不同的回應
            return item.RequestPath switch
            {
                "/api/data" => $"{{\"data\": \"Processed data for request {item.Id}\", \"timestamp\": \"{DateTime.UtcNow:O}\"}}",
                "/api/calculate" => $"{{\"result\": {new Random().Next(1, 1000)}, \"requestId\": \"{item.Id}\"}}",
                _ => $"{{\"message\": \"Request {item.Id} processed successfully\", \"path\": \"{item.RequestPath}\"}}"
            };
        }
    }
}
