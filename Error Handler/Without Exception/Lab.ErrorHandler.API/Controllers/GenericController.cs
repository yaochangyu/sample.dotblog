using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Lab.ErrorHandler.API.Controllers;

public class GenericController : ControllerBase
{
    private static readonly Lazy<Dictionary<FailureCode, int>> s_failureLookupLazy = new(() => new()
    {
        { FailureCode.InputInvalid, StatusCodes.Status400BadRequest },
        { FailureCode.MemberAlreadyExist, StatusCodes.Status400BadRequest },
        { FailureCode.MemberNotFound, StatusCodes.Status404NotFound },
        { FailureCode.DataNotFound, StatusCodes.Status404NotFound },
        { FailureCode.DataConcurrency, StatusCodes.Status429TooManyRequests },
        { FailureCode.ServerError, StatusCodes.Status500InternalServerError },
        { FailureCode.DbError, StatusCodes.Status500InternalServerError },
        { FailureCode.S3Error, StatusCodes.Status500InternalServerError },
    });

    private static readonly Dictionary<FailureCode, int> FailureLookup = s_failureLookupLazy.Value;

    [NonAction]
    public FailureObjectResult GenericFailure(Failure failure)
    {
        if (string.IsNullOrWhiteSpace(failure.TraceId))
        {
            failure.TraceId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier;
        }

        if (FailureLookup.TryGetValue(failure.Code, out int statusCode))
        {
            return new FailureObjectResult(failure, statusCode);
        }

        return new FailureObjectResult(failure);
    }
}