using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using EsDailyLogs.Models;

namespace EsDailyLogs.Services;

public interface ITraditionalLogService
{
    Task<bool> WriteLogAsync(LogEntry log);
    Task<IReadOnlyCollection<LogEntry>> QueryLogsAsync(string? service, string? keyword, DateTime from, DateTime to, int size = 50);
}

public class TraditionalLogService : ITraditionalLogService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<TraditionalLogService> _logger;

    public TraditionalLogService(ElasticsearchClient client, ILogger<TraditionalLogService> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// 傳統寫法：每次寫入需在程式碼中動態組裝當天的 Index 名稱（如 logs-app-2026.08.23）
    /// </summary>
    public async Task<bool> WriteLogAsync(LogEntry log)
    {
        var dailyIndex = $"logs-app-{log.Timestamp:yyyy.MM.dd}";

        var response = await _client.IndexAsync(log, idx => idx
            .Index(dailyIndex)
        );

        if (!response.IsValidResponse)
        {
            _logger.LogError("傳統寫入失敗: {DebugInfo}", response.DebugInformation);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 傳統寫法：跨日查詢時，必須手動計算日期範圍包含哪些單日索引
    /// </summary>
    public async Task<IReadOnlyCollection<LogEntry>> QueryLogsAsync(
        string? service,
        string? keyword,
        DateTime from,
        DateTime to,
        int size = 50)
    {
        // 1. 手動計算跨日所有的索引名稱
        var targetIndices = new List<string>();
        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            targetIndices.Add($"logs-app-{date:yyyy.MM.dd}");
        }

        var filters = new List<Query>
        {
            new DateRangeQuery(new Field("@timestamp"))
            {
                Gte = from.ToString("o"),
                Lte = to.ToString("o")
            }
        };

        if (!string.IsNullOrWhiteSpace(service))
        {
            filters.Add(new MatchQuery(Infer.Field<LogEntry>(f => f.Service)) { Query = service });
        }

        var mustQueries = new List<Query>();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            mustQueries.Add(new MatchQuery(Infer.Field<LogEntry>(f => f.Message)) { Query = keyword });
        }

        // 2. 指定多個每日索引進行查詢
        var response = await _client.SearchAsync<LogEntry>(s => s
            .Indices(targetIndices.Select(x => (IndexName)x).ToArray())
            .AllowNoIndices(true)
            .IgnoreUnavailable(true)
            .Size(size)
            .Sort(sort => sort.Field(new Field("@timestamp"), new FieldSort { Order = SortOrder.Desc }))
            .Query(new BoolQuery
            {
                Filter = filters,
                Must = mustQueries.Count > 0 ? mustQueries : null
            })
        );

        if (!response.IsValidResponse)
        {
            _logger.LogError("傳統查詢失敗: {DebugInfo}", response.DebugInformation);
            return Array.Empty<LogEntry>();
        }

        var result = new List<LogEntry>();
        foreach (var hit in response.Hits)
        {
            if (hit.Source != null)
            {
                hit.Source.Id = hit.Id;
                result.Add(hit.Source);
            }
        }

        return result;
    }
}
