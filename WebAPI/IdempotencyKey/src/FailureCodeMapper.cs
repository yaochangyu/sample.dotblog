using System.Net;

namespace IdempotencyKey.WebApi;

public static class FailureCodeMapper
{
    private static readonly Dictionary<string, HttpStatusCode> CodeMapping = new()
    {
        [nameof(FailureCode.NotFound)]       = HttpStatusCode.NotFound,
        [nameof(FailureCode.DuplicateEmail)] = HttpStatusCode.Conflict,
        [nameof(FailureCode.DbConcurrency)]  = HttpStatusCode.Conflict,
        [nameof(FailureCode.DbError)]        = HttpStatusCode.InternalServerError,
        [nameof(FailureCode.ValidationError)]= HttpStatusCode.BadRequest,
        [nameof(FailureCode.Unknown)]        = HttpStatusCode.InternalServerError,
    };

    public static HttpStatusCode GetHttpStatusCode(string failureCode) =>
        CodeMapping.TryGetValue(failureCode, out var statusCode)
            ? statusCode
            : HttpStatusCode.InternalServerError;

    public static HttpStatusCode GetHttpStatusCode(Failure failure) =>
        GetHttpStatusCode(failure.Code);
}
