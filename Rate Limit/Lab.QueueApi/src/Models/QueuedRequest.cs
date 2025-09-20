namespace Lab.QueueApi.Models;

/// <summary>
/// 代表一個已排入佇列的請求。
/// </summary>
public class QueuedRequest
{
    /// <summary>
    /// 請求的唯一識別碼。
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 請求進入佇列的時間。
    /// </summary>
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 原始請求的資料。
    /// </summary>
    public string RequestData { get; set; } = string.Empty;

    /// <summary>
    /// 用於等待請求完成的 TaskCompletionSource。
    /// </summary>
    public TaskCompletionSource<ApiResponse> CompletionSource { get; set; } = new();
}

/// <summary>
/// 代表 API 的標準回應結構。
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// 指示操作是否成功。
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 提供有關操作結果的訊息。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 回應中包含的資料。
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// 請求處理完成的時間。
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 代表傳入 API 的請求結構。
/// </summary>
public class ApiRequest
{
    /// <summary>
    /// 請求中包含的資料。
    /// </summary>
    public string Data { get; set; } = string.Empty;
}