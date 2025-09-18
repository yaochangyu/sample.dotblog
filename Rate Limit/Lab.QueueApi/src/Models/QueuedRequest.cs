namespace Lab.QueueApi.Models;

public class QueuedRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public string RequestData { get; set; } = string.Empty;
    public TaskCompletionSource<ApiResponse> CompletionSource { get; set; } = new();
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public class CreateQueueRequest
{
    public string Data { get; set; } = string.Empty;
}

