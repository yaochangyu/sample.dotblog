namespace Lab.QueueApi.Services;

/// <summary>
/// 速率限制服務介面
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// 檢查是否可以處理新請求
    /// </summary>
    /// <returns>如果可以處理則返回 true，否則返回 false</returns>
    bool CanProcess();

    /// <summary>
    /// 記錄一個新的請求處理
    /// </summary>
    void RecordRequest();
}