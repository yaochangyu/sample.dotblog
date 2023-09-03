using Microsoft.AspNetCore.Mvc;

namespace Lab.ErrorHandler.API.Controllers;

public class FailureObjectResult : ObjectResult
{
    private ILogger<FailureObjectResult> _logger;

    public FailureObjectResult(Failure error, int statusCode = StatusCodes.Status400BadRequest)
        : base(error)
    {
        this.StatusCode = statusCode;
        this.Value = error.WithoutException();
    }
}