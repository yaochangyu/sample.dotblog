using Elastic.Clients.Elasticsearch;
using EsDailyLogs.Models;

namespace EsDailyLogs.Services;

public class LogBatchProcessor : BackgroundService
{
    private readonly ILogQueue _queue;
    private readonly ElasticsearchClient _client;
    private readonly ILogger<LogBatchProcessor> _logger;

    private const int BatchSize = 100; // 測試環境調為 100
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMilliseconds(500); // 測試環境 500ms
    public const string TargetDataStream = "logs-app-prod";

    public LogBatchProcessor(
        ILogQueue queue,
        ElasticsearchClient client,
        ILogger<LogBatchProcessor> logger)
    {
        _queue = queue;
        _client = client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var buffer = new List<LogEntry>(BatchSize);
        var lastFlushTime = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await foreach (var item in _queue.ReadAllAsync(stoppingToken))
                {
                    buffer.Add(item);

                    bool isOverdue = DateTime.UtcNow - lastFlushTime >= FlushInterval;
                    if (buffer.Count >= BatchSize || isOverdue)
                    {
                        await _01_批次寫入Elasticsearch(buffer);
                        buffer.Clear();
                        lastFlushTime = DateTime.UtcNow;
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批次處理過程發生未預期錯誤");
                await Task.Delay(500, stoppingToken);
            }
        }

        if (buffer.Count > 0)
        {
            await _01_批次寫入Elasticsearch(buffer);
        }
    }

    private async Task _01_批次寫入Elasticsearch(List<LogEntry> logs)
    {
        if (logs.Count == 0) return;

        var response = await _client.BulkAsync(b => b
            .Index(TargetDataStream)
            .CreateMany(logs)
        );

        if (!response.IsValidResponse)
        {
            _logger.LogError("Bulk 寫入失敗: {DebugInfo}", response.DebugInformation);
        }
        else
        {
            _logger.LogInformation("成功批次寫入 {Count} 筆資料至 ES", logs.Count);
        }
    }
}
