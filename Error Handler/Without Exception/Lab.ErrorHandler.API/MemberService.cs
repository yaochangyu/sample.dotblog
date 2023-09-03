using System.Data;
using FluentValidation;
using Lab.ErrorHandler.API.Extensions;
using Lab.ErrorHandler.API.Models;

namespace Lab.ErrorHandler.API;

public class MemberService
{
    private readonly IValidator<BindCellphoneRequest> _validator;

    public MemberService(IValidator<BindCellphoneRequest> validator)
    {
        this._validator = validator;
    }

    //一個方法有多種可能的 Failure
    public async Task<bool> BindCellphoneAsync(BindCellphoneRequest request,
        CancellationToken cancel = default)
    {
        var validationResult = await this._validator.ValidateAsync(request, cancel);
        if (validationResult.IsValid == false)
        {
            return false;
        }

        try
        {
            //找不到會員
            var getMemberResult = await this.GetMemberAsync(request.MemberId, cancel);
        }
        catch (Exception e)
        {
        }

        try
        {
            //手機格式無效
            var validateCellphoneResult = await this.ValidateCellphoneAsync(request.Cellphone, cancel);
        }
        catch (Exception e)
        {
        }

        try
        {
            //資料衝突，手機已經被綁定
            var saveChangeResult = await this.SaveChangeAsync(request, cancel);
        }
        catch (Exception e)
        {
        }

        return true;
    }

    public async Task<(Failure Failure, bool Data)> SaveChangeAsync(BindCellphoneRequest request,
        CancellationToken cancel = default)
    {
        throw new DBConcurrencyException("insert data row concurrency error.");
    }

    public async Task<bool> ValidateCellphoneAsync(string cellphone,
        CancellationToken cancel = default)
    {
        throw new Exception("Cellphone format invalid.");
    }

    // public async Task<GetMemberResult> GetMemberAsync(int memberId,
    //     CancellationToken cancel = default)
    // {
    //     try
    //     {
    //         throw new Exception("Member not found.");
    //     }
    //     catch (Exception e)
    //     {
    //         throw;
    //     }
    // }
    public async Task<(Failure Failure,GetMemberResult Data)> GetMemberAsync(int memberId,
        CancellationToken cancel = default)
    {
        try
        {
            throw new Exception("Member not found.");
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

    //具有多個 Detail 的 Failure
    public async Task<(Failure Failure, bool Data)> CreateMemberAsync(CreateMemberRequest request,
        CancellationToken cancel = default)
    {
        var failure = new Failure()
        {
            Code = FailureCode.InputInvalid,
            Message = "view detail errors",
            Details = new List<Failure>()
            {
                new(code: FailureCode.InputInvalid, message: "Input invalid."),
                new(code: FailureCode.CellphoneFormatInvalid, message: "Cellphone format invalid."),
                new(code: FailureCode.DataConflict, message: "Member already exist."),
            }
        };
        return (failure, false);
    }
}