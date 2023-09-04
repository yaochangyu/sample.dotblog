using System.Data;
using Lab.ErrorHandler.API.Models;

namespace Lab.ErrorHandler.API;

static class MemberWorkflowExtensions
{
    public static async Task<(Failure Failure, GetMemberResult Data)> GetMemberAsync<TSource>(
        this Task<(Failure Failure, TSource Data)> previousStep,
        int memberId,
        CancellationToken cancel = default)
    {
        try
        {
            var previousStepResult = await previousStep;
            if (previousStepResult.Failure != null)
            {
                return (previousStepResult.Failure, null);
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

    public static async Task<(Failure Failure, bool Data)> SaveChangeAsync<TSource>(
        this Task<(Failure Failure, TSource Data)> previousStep,
        BindCellphoneRequest request,
        CancellationToken cancel = default)
    {
        try
        {
            var previousStepResult = await previousStep;
            if (previousStepResult.Failure != null)
            {
                return (previousStepResult.Failure, false);
            }

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

    public static async Task<(Failure Failure, bool Data)> ValidateCellphoneAsync<TSource>(
        this Task<(Failure Failure, TSource Data)> previousStep,
        string cellphone,
        CancellationToken cancel = default)
    {
        var previousStepResult = await previousStep;
        if (previousStepResult.Failure != null)
        {
            return (previousStepResult.Failure, false);
        }

        return (new Failure
        {
            Code = FailureCode.CellphoneFormatInvalid,
            Message = "Cellphone format invalid.",
            Data = cellphone
        }, false);
    }
}