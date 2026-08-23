using Elastic.Clients.Elasticsearch;
using EsDailyLogs.Models;
using EsDailyLogs.Services;

var builder = WebApplication.CreateBuilder(args);

var settings = new ElasticsearchClientSettings(new Uri("http://localhost:9200"))
    .MaximumRetries(3)
    .RequestTimeout(TimeSpan.FromSeconds(30));

builder.Services.AddSingleton(new ElasticsearchClient(settings));
builder.Services.AddSingleton<ILogQueue, LogQueue>();
builder.Services.AddHostedService<LogBatchProcessor>();
builder.Services.AddScoped<ILogService, LogService>();

var app = builder.Build();

// [Create] 寫入 Log
app.MapPost("/api/logs", async (LogEntry entry, ILogQueue queue) =>
{
    entry.Timestamp = DateTime.UtcNow;
    await queue.EnqueueAsync(entry);
    return Results.Accepted();
});

// [Read] 依 ID 取得單筆 Log
app.MapGet("/api/logs/{id}", async (string id, ILogService service) =>
{
    var log = await service.GetByIdAsync(id);
    return log != null ? Results.Ok(log) : Results.NotFound();
});

// [Read] 依條件搜尋 Logs
app.MapGet("/api/logs", async (
    string? service,
    string? keyword,
    DateTime? from,
    DateTime? to,
    int? size,
    ILogService logService) =>
{
    var startTime = from ?? DateTime.UtcNow.AddHours(-24);
    var endTime = to ?? DateTime.UtcNow.AddMinutes(5);
    var pageSize = size ?? 50;

    var logs = await logService.QueryLogsAsync(service, keyword, startTime, endTime, pageSize);
    return Results.Ok(logs);
});

// [Update] 修改 Log 內容
app.MapPut("/api/logs/{index}/{id}", async (
    string index,
    string id,
    UpdateLogRequest req,
    ILogService logService) =>
{
    var success = await logService.UpdateLogMessageAsync(index, id, req.Message);
    return success ? Results.NoContent() : Results.BadRequest();
});

// [Delete] 刪除 Log
app.MapDelete("/api/logs/{index}/{id}", async (
    string index,
    string id,
    ILogService logService) =>
{
    var success = await logService.DeleteLogAsync(index, id);
    return success ? Results.NoContent() : Results.NotFound();
});

app.Run();

public record UpdateLogRequest(string Message);

public partial class Program { }
