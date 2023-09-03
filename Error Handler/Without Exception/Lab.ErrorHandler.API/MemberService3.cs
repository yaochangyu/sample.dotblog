using System.Data;
using FluentValidation;
using Lab.ErrorHandler.API.Extensions;
using Lab.ErrorHandler.API.Models;

namespace Lab.ErrorHandler.API;

public class MemberService3
{
    private readonly IValidator<BindCellphoneRequest> _validator;

    public MemberService3(IValidator<BindCellphoneRequest> validator)
    {
        this._validator = validator;
    }

    //一個方法有多種可能的 Failure
    // public async Task<(Failure Failure, bool Data)> BindCellphoneAsync(BindCellphoneRequest request,
    //     CancellationToken cancel = default) =>
    //     await this.ValidateModelAsync(request, cancel)
    //         .ThenWithFailureAsync(p => p.Failure != null
    //             ? Task.FromResult((p.Failure, false))
    //             : this.ValidateCellphoneAsync(request.Cellphone, cancel))
    //         .ThenWithFailureAsync(p => p.Failure != null
    //             ? Task.FromResult((p.Failure, false))
    //             : this.GetMemberAsync(request.MemberId, cancel))
    //         .ThenWithFailureAsync(p => p.Failure != null
    //             ? Task.FromResult((p.Failure, false))
    //             : this.SaveChangeAsync(request, cancel))
    //     ;
    public async Task<(Failure Failure, bool Data)> BindCellphoneAsync(BindCellphoneRequest request,
        CancellationToken cancel = default) =>
        await this.ValidateModelAsync(request, cancel)
            .ThenAsyncIfNoFailure(p => this.GetMemberAsync(request.MemberId, cancel))
            .ThenAsyncIfNoFailure(p => this.ValidateCellphoneAsync(p.Cellphone, cancel))
            .ThenAsyncIfNoFailure(p => this.SaveChangeAsync(request, cancel));

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

    public async Task<(Failure Failure, bool Data)> SaveChangeAsync(BindCellphoneRequest request,
        CancellationToken cancel = default)
    {
        try
        {
            throw new DBConcurrencyException("insert data row concurrency error.");
        }
        catch (Exception e)
        {
            return (new Failure
            {
                Code = FailureCode.DataConcurrency,
                Message = e.Message,
                Exception = e,
                Data = request
            }, false);
        }
    }

    public async Task<(Failure Failure, bool Data)> ValidateCellphoneAsync(string cellphone,
        CancellationToken cancel = default)
    {
        return (new Failure
        {
            Code = FailureCode.CellphoneFormatInvalid,
            Message = "Cellphone format invalid.",
            Data = cellphone
        }, false);
    }

    public async Task<(Failure Failure, GetMemberResult Data)> GetMemberAsync(int memberId,
        CancellationToken cancel = default)
    {
        try
        {
            throw new Exception($"can not connect db.");
        }
        catch (Exception e)
        {
            return (new Failure
            {
                Code = FailureCode.DbError,
                Message = e.Message,
                Data = memberId,
                Exception = e,
            }, null);
        }
    }
}