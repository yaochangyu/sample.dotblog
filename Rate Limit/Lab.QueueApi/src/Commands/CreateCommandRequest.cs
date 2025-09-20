namespace Lab.QueueApi.Commands;

/// <summary>
/// 建立任務請求
/// </summary>
public class CreateCommandRequest
{
    /// <summary>
    /// 請求中包含的資料。
    /// </summary>
    public string Data { get; set; } = string.Empty;
}