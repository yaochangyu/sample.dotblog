using System.Text.Json.Serialization;

namespace EsDailyLogs.Models;

public class LogEntry
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }

    [JsonPropertyName("@timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string Service { get; set; } = string.Empty;

    public string Level { get; set; } = "Information";

    public string Message { get; set; } = string.Empty;

    public string TraceId { get; set; } = string.Empty;
}
