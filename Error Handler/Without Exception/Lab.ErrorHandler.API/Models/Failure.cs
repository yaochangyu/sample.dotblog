using System.Text.Json.Serialization;

namespace Lab.ErrorHandler.API.Models;

public class Failure
{
    public Failure()
    {
    }

    public Failure(FailureCode code, string message)
    {
        this.Code = code;
        this.Message = message;
    }

    /// <summary>
    /// 錯誤碼
    /// </summary>
    public FailureCode Code { get; init; }

    /// <summary>
    /// 錯誤訊息
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// 錯誤發生時的資料
    /// </summary>
    public object Data { get; init; }

    /// <summary>
    /// 追蹤 Id
    /// </summary>
    public string TraceId { get; set; }

    /// <summary>
    /// 例外，不回傳給 Web API 
    /// </summary>
    [JsonIgnore]
    public Exception Exception { get; set; }

    public List<Failure> Details { get; init; } = new();

    //用了 [JsonIgnore] 似乎就不需要它了 QQ，寫完了才想到可以 Ignore，不過這仍然可以適用在其他場景，例如 CLI、Console App
    public Failure WithoutException()
    {
        List<Failure> details = new();
        foreach (var detail in this.Details)
        {
            details.Add(this.WithoutException(detail));
        }

        return new Failure(this.Code, this.Message)
        {
            Data = this.Data,
            Details = details,
            TraceId = this.TraceId,
        };
    }

    public Failure WithoutException(Failure error)
    {
        var result = new Failure(error.Code, error.Message)
        {
            TraceId = error.TraceId
        };

        foreach (var detailError in this.Details)
        {
            // 遞迴處理 Details 屬性
            var detailResult = this.WithoutException(detailError);
            result.Details.Add(detailResult);
        }

        return result;
    }
}