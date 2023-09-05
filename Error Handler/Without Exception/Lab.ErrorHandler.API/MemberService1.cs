using System.Data;
using FluentValidation;
using Lab.ErrorHandler.API.Extensions;
using Lab.ErrorHandler.API.Models;

namespace Lab.ErrorHandler.API;

public class MemberService1
{
    private readonly IValidator<BindCellphoneRequest> _validator;

    public MemberService1(IValidator<BindCellphoneRequest> validator)
    {
        this._validator = validator;
    }

    //一個方法有多種可能的 Failure
    public async Task<(Failure Failure, bool Data)> BindCellphoneAsync(BindCellphoneRequest request,
        CancellationToken cancel = default)
    {
        var validationResult = await this._validator.ValidateAsync(request, cancel);
        if (validationResult.IsValid == false)
        {
            return (validationResult.ToFailure(), false);
        }

        //找不到會員
        var getMemberResult = await this.GetMemberAsync(request.MemberId, cancel);
        if (getMemberResult.Failure != null)
        {
            return (getMemberResult.Failure, false);
        }

        //手機格式無效
        var validateCellphoneResult = await this.ValidateCellphoneAsync(getMemberResult.Data.Cellphone, cancel);
        if (validateCellphoneResult.Failure != null)
        {
            return validateCellphoneResult;
        }

        //資料衝突，手機已經被綁定
        var saveChangeResult = await this.SaveChangeAsync(request, cancel);

        return saveChangeResult;
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
            if (memberId == 1)
            {
                //模擬找不到資料所回傳的失敗
                return (new Failure
                {
                    Code = FailureCode.MemberNotFound,
                    Message = "member not found.",
                }, null);
            }

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