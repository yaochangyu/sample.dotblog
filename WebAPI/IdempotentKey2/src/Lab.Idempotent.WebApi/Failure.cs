using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Lab.Idempotent.WebApi;

public enum FailureCode
{
    NotFoundIdempotentKey,
}

public class Failure
{
    public static IReadOnlyDictionary<FailureCode, ObjectResult> Results = new Dictionary<FailureCode, ObjectResult>()
    {
        {
            FailureCode.NotFoundIdempotentKey,new ObjectResult(new Failure
            {
                Code = FailureCode.NotFoundIdempotentKey.ToString(),
                Message = "Not found Idempotent key in header",
                Data = new
                {
                    Property = "IdempotentKey",
                    Value = ""
                },
            })
            {
                StatusCode = (int)HttpStatusCode.BadRequest
            }
        },
    };

    public string Code { get; set; }

    public string Message { get; set; }

    public object Data { get; set; }

    public IEnumerable<Failure> Failures { get; set; }
}
