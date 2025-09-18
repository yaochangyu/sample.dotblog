namespace Lab.QueueApi.Models;

/// <summary>
/// 表示 API 請求的資料模型
/// </summary>
public class ApiRequest
{
    /// <summary>
    /// 請求的主要資料內容
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// 可選的請求參數字典
    /// </summary>
    public Dictionary<string, string>? Parameters { get; set; }
}