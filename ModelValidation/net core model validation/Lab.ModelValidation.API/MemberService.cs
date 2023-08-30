using FluentValidation;
using Lab.ModelValidation.API.Extensions;
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
            var failure = validateResult.ToFailure();
            return (failure, false);
        }

        return (null, true);
    }
}