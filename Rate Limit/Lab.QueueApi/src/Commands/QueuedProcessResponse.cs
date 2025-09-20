namespace Lab.QueueApi.Commands;

public class QueuedCommandResponse
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