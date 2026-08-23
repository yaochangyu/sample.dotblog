using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using EsDailyLogs.Models;

namespace EsDailyLogs.Services;

public interface ILogService
{
    Task<LogEntry?> GetByIdAsync(string id);
    Task<IReadOnlyCollection<LogEntry>> QueryLogsAsync(string? service, string? keyword, DateTime from, DateTime to, int size = 50);
    Task<bool> UpdateLogMessageAsync(string indexName, string id, string newMessage);
    Task<bool> DeleteLogAsync(string indexName, string id);
}

public class LogService : ILogService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<LogService> _logger;
    public const string TargetDataStream = "logs-app-prod";

    public LogService(ElasticsearchClient client, ILogger<LogService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<LogEntry?> GetByIdAsync(string id)
    {
        var response = await _client.SearchAsync<LogEntry>(s => s
            .Indices(TargetDataStream)
            .Query(new IdsQuery { Values = new Ids(id) })
            .Size(1)
        );

        if (response.IsValidResponse && response.Hits.Count > 0)
        {
            var hit = response.Hits.First();
            var log = hit.Source;
            if (log != null)
            {
                log.Id = hit.Id;
            }
            return log;
        }

        return null;
    }

    public async Task<IReadOnlyCollection<LogEntry>> QueryLogsAsync(
        string? service,
        string? keyword,
        DateTime from,
        DateTime to,
        int size = 50)
    {
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

        var response = await _client.SearchAsync<LogEntry>(s => s
            .Indices(TargetDataStream)
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
            _logger.LogError("查詢失敗: {DebugInfo}", response.DebugInformation);
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

    public async Task<bool> UpdateLogMessageAsync(string indexName, string id, string newMessage)
    {
        var response = await _client.UpdateAsync<LogEntry, object>(indexName, id, u => u
            .Doc(new { message = newMessage })
        );

        if (!response.IsValidResponse)
        {
            _logger.LogError("更新失敗: {DebugInfo}", response.DebugInformation);
        }

        return response.IsValidResponse;
    }

    public async Task<bool> DeleteLogAsync(string indexName, string id)
    {
        var response = await _client.DeleteAsync<LogEntry>(id, d => d.Index(indexName));
        if (!response.IsValidResponse)
        {
            _logger.LogError("刪除失敗: {DebugInfo}", response.DebugInformation);
        }
        return response.IsValidResponse;
    }
}
