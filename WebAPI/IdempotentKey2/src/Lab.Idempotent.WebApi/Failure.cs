using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Lab.Idempotent.WebApi;

public enum FailureCode
{
    NotFoundIdempotentKey,
    IdempotentKeyPayloadMismatch,
    ConcurrentRequest,
}

public class Failure
{
    // 每次呼叫建立新的 ObjectResult 實例，避免跨請求共用可變物件造成 race condition
    public static ObjectResult NewResult(FailureCode code) => code switch
    {
        FailureCode.NotFoundIdempotentKey => new ObjectResult(new Failure
        {
            Code = code.ToString(),
            Message = "Not found Idempotent key in header",
            Data = new { Property = "Idempotency-Key", Value = "" },
        }) { StatusCode = (int)HttpStatusCode.BadRequest },

        FailureCode.IdempotentKeyPayloadMismatch => new ObjectResult(new Failure
        {
            Code = code.ToString(),
            Message = "Idempotency-Key is already used with a different request payload",
            Data = new { Property = "Idempotency-Key", Value = "" },
        }) { StatusCode = (int)HttpStatusCode.UnprocessableEntity },

        FailureCode.ConcurrentRequest => new ObjectResult(new Failure
        {
            Code = code.ToString(),
            Message = "A request with the same Idempotency-Key is currently being processed",
            Data = new { Property = "Idempotency-Key", Value = "" },
        }) { StatusCode = (int)HttpStatusCode.Conflict },

        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
    };

    public required string Code { get; set; }

    public required string Message { get; set; }

    public object? Data { get; set; }

    public IEnumerable<Failure>? Failures { get; set; }
}
