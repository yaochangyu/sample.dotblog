using System.Text.Json.Serialization;

namespace Lab.Sharding.WebAPI;

public class Failure
{
    public Failure()
    {
    }

    public Failure(string code, string message)
    {
        this.Code = code;
        this.Message = message;
    }

    /// <summary>
    /// 錯誤碼
    /// </summary>
    public string Code { get; init; }

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
}