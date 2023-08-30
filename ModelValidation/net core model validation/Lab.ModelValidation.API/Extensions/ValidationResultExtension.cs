using FluentValidation.Results;

namespace Lab.ModelValidation.API.Extensions;

static class ValidationResultExtension
{
    public static Failure ToFailure(this ValidationResult validateResult)
    {
        if (validateResult.IsValid)
        {
            return null;
        }

        var errors = validateResult.Errors
            .ToDictionary(p => p.PropertyName, p => p.ErrorMessage);
        var failure = new Failure()
        {
            Code = FailureCode.InputInvalid,
            Message = "input invalid",
            Data = errors,
        };
        return failure;
    }
}