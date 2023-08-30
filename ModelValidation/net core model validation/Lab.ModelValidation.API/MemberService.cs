using FluentValidation;
using Lab.ModelValidation.API.Models;

namespace Lab.ModelValidation.API;

public class MemberService
{
    private readonly IValidator<CreateMemberRequest> _validator;

    public MemberService(IValidator<CreateMemberRequest> validator)
    {
        this._validator = validator;
    }

    public async Task<(Failure Failure, bool Data)> CreateMemberAsync(CreateMemberRequest request,
        CancellationToken cancel = default)
    {
        var validateResult = await this._validator.ValidateAsync(request, cancel);
        if (validateResult.IsValid == false)
        {
            var errors = validateResult.Errors
                .ToDictionary(p => p.PropertyName, p => p.ErrorMessage);
            var failure = new Failure()
            {
                Code = FailureCode.InputInvalid,
                Message = "input invalid",
                Data = errors,
            };
            return (failure, false);
        }

        return (null, true);
    }
}