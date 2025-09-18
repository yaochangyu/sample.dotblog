using Lab.QueueApi.Models;

namespace Lab.QueueApi.Services;

/// <summary>
/// 請求處理服務介面
/// </summary>
public interface IRequestProcessor
{
    /// <summary>
    /// 異步處理請求
    /// </summary>
    /// <param name="request">要處理的請求</param>
    /// <returns>處理結果</returns>
    Task<object> ProcessRequestAsync(ApiRequest request);
}