using Lab.QueueApi.Models;

namespace Lab.QueueApi.Services;

/// <summary>
/// 請求處理服務實作
/// </summary>
public class RequestProcessor : IRequestProcessor
{
    /// <summary>
    /// 異步處理請求
    /// </summary>
    /// <param name="request">要處理的請求</param>
    /// <returns>處理結果</returns>
    public async Task<object> ProcessRequestAsync(ApiRequest request)
    {
        // 模擬處理時間
        await Task.Delay(100);

        // 返回處理結果
        return new
        {
            Success = true,
            Data = $"Processed: {request.Data}",
            ProcessedAt = DateTime.UtcNow,
            RequestParameters = request.Parameters
        };
    }
}