using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Lab.Idempotent.WebApi;

public enum FailureCode
{
    NotFoundIdempotentKey,
    IdempotentKeyPayloadMismatch,
}

public class Failure
{
    public static IReadOnlyDictionary<FailureCode, ObjectResult> Results = new Dictionary<FailureCode, ObjectResult>()
    {
        {
            FailureCode.NotFoundIdempotentKey, new ObjectResult(new Failure
            {
                Code = FailureCode.NotFoundIdempotentKey.ToString(),
                Message = "Not found Idempotent key in header",
                Data = new
                {
                    Property = "Idempotency-Key",
                    Value = ""
                },
            })
            {
                StatusCode = (int)HttpStatusCode.BadRequest
            }
        },
        {
            FailureCode.IdempotentKeyPayloadMismatch, new ObjectResult(new Failure
            {
                Code = FailureCode.IdempotentKeyPayloadMismatch.ToString(),
                Message = "Idempotency-Key is already used with a different request payload",
                Data = new
                {
                    Property = "Idempotency-Key",
                    Value = ""
                },
            })
            {
                StatusCode = (int)HttpStatusCode.UnprocessableEntity
            }
        },
    };

    public string Code { get; set; }

    public string Message { get; set; }

    public object Data { get; set; }

    public IEnumerable<Failure> Failures { get; set; }
}
