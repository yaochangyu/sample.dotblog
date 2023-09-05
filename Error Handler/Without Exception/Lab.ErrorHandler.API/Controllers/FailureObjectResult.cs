using Lab.ErrorHandler.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lab.ErrorHandler.API.Controllers;

public class FailureObjectResult : ObjectResult
{
    public FailureObjectResult(Failure failure, int statusCode = StatusCodes.Status400BadRequest)
        : base(failure)
    {
        this.StatusCode = statusCode;
        // Failure.Exception 已經使用 [JsonIgnore]，不會再回傳給調用端
        // this.Value = failure.WithoutException();
        this.Value = failure;
    }
}