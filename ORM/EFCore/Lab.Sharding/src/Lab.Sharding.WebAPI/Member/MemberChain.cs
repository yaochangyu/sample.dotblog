using CSharpFunctionalExtensions;
using Lab.Sharding.Infrastructure.TraceContext;

namespace Lab.Sharding.WebAPI.Member;

public static class MemberChain
{

    // 檢查是否有重複的 Email
    public static Result<Member, Failure>
        ValidateEmail(this Result<Member, Failure> previousResult,
                      InsertMemberRequest dest)
    {
        if (previousResult.IsFailure)
        {
            return previousResult;
        }

        var src = previousResult.Value;
        if (src == null)
        {
            return Result.Success<Member, Failure>(src);
        }

        if (src.Email == dest.Email)
        {
            return Result.Failure<Member, Failure>(new Failure
            {
                Code = nameof(FailureCode.DuplicateEmail),
                Message = "Email 重複",
                Data = src,
            });
        }

        return Result.Success<Member, Failure>(src);
    }

    // 檢查是否有重複的 Email
    public static Result<Member, Failure>
        ValidateName(this Result<Member, Failure> previousResult,
                     InsertMemberRequest dest)
    {
        if (previousResult.IsFailure)
        {
            return previousResult;
        }

        var src = previousResult.Value;
        if (src == null)
        {
            return Result.Success<Member, Failure>(src);
        }

        if (src.Email == dest.Email)
        {
            return Result.Failure<Member, Failure>(new Failure
            {
                Code = nameof(FailureCode.DuplicateEmail),
                Message = "Email 重複",
                Data = src,
            });
        }

        return Result.Success<Member, Failure>(src);
    }
}