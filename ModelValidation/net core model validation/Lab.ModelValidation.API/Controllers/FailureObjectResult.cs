using Microsoft.AspNetCore.Mvc;

namespace Lab.ModelValidation.API.Controllers;

public class FailureObjectResult : ObjectResult
{
    public FailureObjectResult(Failure error, int statusCode = StatusCodes.Status400BadRequest)
        : base(error)
    {
        this.StatusCode = statusCode;
        this.Value = error;
    }
}