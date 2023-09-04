using FluentValidation;
using Lab.ErrorHandler.API.Extensions;
using Lab.ErrorHandler.API.Models;

namespace Lab.ErrorHandler.API;

public class MemberService4
{
    private readonly IValidator<BindCellphoneRequest> _validator;

    public MemberService4(IValidator<BindCellphoneRequest> validator)
    {
        this._validator = validator;
    }

    //一個方法有多種可能的 Failure
    public async Task<(Failure Failure, bool Data)> BindCellphoneAsync(BindCellphoneRequest request,
        CancellationToken cancel = default)
    {
        var executeResult = await this.ValidateModelAsync(request, cancel)
            .GetMemberAsync(request.MemberId, cancel)
            .ValidateCellphoneAsync(request.Cellphone, cancel)
            .SaveChangeAsync(request, cancel);
        if (executeResult.Failure != null)
        {
            return (executeResult.Failure, false);
        }

        return (null, true);
    }

    public async Task<(Failure Failure, bool Data)> ValidateModelAsync(BindCellphoneRequest request,
        CancellationToken cancel = default)
    {
        var validationResult = await this._validator.ValidateAsync(request, cancel);
        if (validationResult.IsValid == false)
        {
            return (validationResult.ToFailure(), false);
        }

        return (null, true);
    }
}