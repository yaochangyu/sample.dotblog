using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;

namespace IdempotencyKey.WebApi;

public class ActionResult<TSuccess, TFailure> : ActionResult
    where TFailure : class
{
    private readonly Result<TSuccess, TFailure> _result;

    public ActionResult(Result<TSuccess, TFailure> result)
    {
        _result = result;
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
        var objectResult = _result.IsSuccess
            ? CreateSuccessResult(_result.Value)
            : CreateFailureResult(_result.Error);

        await objectResult.ExecuteResultAsync(context);
    }

    public ObjectResult CreateSuccessResult(TSuccess value)
    {
        return new ObjectResult(value) { StatusCode = StatusCodes.Status200OK };
    }

    public ObjectResult CreateFailureResult(TFailure error)
    {
        if (error is Failure failure)
        {
            var statusCode = FailureCodeMapper.GetHttpStatusCode(failure);
            return new ObjectResult(error) { StatusCode = (int)statusCode };
        }

        return new ObjectResult(error) { StatusCode = StatusCodes.Status500InternalServerError };
    }
}

public static class ResultExtensions
{
    public static ActionResult<TSuccess, TFailure> ToActionResult<TSuccess, TFailure>(
        this Result<TSuccess, TFailure> result)
        where TFailure : class
    {
        return new ActionResult<TSuccess, TFailure>(result);
    }

    public static ObjectResult ToSuccessResult<TSuccess, TFailure>(
        this Result<TSuccess, TFailure> result)
        where TFailure : class
    {
        var apiActionResult = new ActionResult<TSuccess, TFailure>(result);
        return apiActionResult.CreateSuccessResult(result.Value);
    }

    public static ObjectResult ToFailureResult<TSuccess, TFailure>(
        this Result<TSuccess, TFailure> result)
        where TFailure : class
    {
        var apiActionResult = new ActionResult<TSuccess, TFailure>(result);
        return apiActionResult.CreateFailureResult(result.Error);
    }
}