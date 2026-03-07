using System.Text.Json.Serialization;

namespace IdempotencyKey.WebApi;

public class Failure
{
    public Failure() { }

    public Failure(string code, string message)
    {
        Code = code;
        Message = message;
    }

    /// <summary>錯誤碼</summary>
    public string Code { get; init; } = nameof(FailureCode.Unknown);

    /// <summary>錯誤訊息</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>錯誤發生時的相關資料</summary>
    public object? Data { get; init; }

    /// <summary>追蹤 Id</summary>
    public string? TraceId { get; init; }

    /// <summary>原始例外（不回傳給 API 呼叫端）</summary>
    [JsonIgnore]
    public Exception? Exception { get; init; }
}
