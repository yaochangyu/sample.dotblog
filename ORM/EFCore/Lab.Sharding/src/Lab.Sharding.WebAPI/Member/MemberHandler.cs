using CSharpFunctionalExtensions;
using Lab.Sharding.Infrastructure.TraceContext;

namespace Lab.Sharding.WebAPI.Member;

public class MemberHandler(
    MemberRepository repository,
    IContextGetter<TraceContext?> traceContextGetter,
    ILogger<MemberHandler> logger)
{
    // public async Task<Result<Member, Failure>>
    //     InsertAsync(InsertMemberRequest request,
    //                 CancellationToken cancel = default)
    // {
    //     var traceContext = traceContextGetter.Get();
    //     var srcMember = await repository.QueryEmailAsync(request.Email, cancel);
    //
    //     //前置條件檢查，可以用 Fluent Pattern 重構
    //     var validateResult = Result.Success<Member, Failure>(srcMember);
    //     validateResult = ValidateEmail(validateResult, request);
    //     validateResult = ValidateName(validateResult, request);
    //     if (validateResult.IsFailure)
    //     {
    //         return validateResult;
    //     }
    //
    //     try
    //     {
    //         var count = await repository.InsertAsync(request, cancel);
    //         var success = Result.Success<Member, Failure>(srcMember);
    //         return success;
    //
    //         //發送 Event 給 MQ
    //     }
    //     catch (Exception e)
    //     {
    //         //各自處理例外，處理過就不要再次 throw
    //         //模擬插資料失敗
    //         var failure = new Failure
    //         {
    //             Code = nameof(FailureCode.DbError),
    //             Message = "資料庫錯誤",
    //             Data = request,
    //             Exception = e,
    //             TraceId = traceContext.TraceId
    //         };
    //
    //         logger.LogError($"{failure}", e);
    //         var failed = Result.Failure<Member, Failure>(failure);
    //         return failed;
    //     }
    // }

    // 檢查是否有重複的 Email
    private static Result<Member, Failure>
        ValidateEmail(Result<Member, Failure> previousResult,
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
    private static Result<Member, Failure>
        ValidateName(Result<Member, Failure> previousResult,
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

    public async Task<PaginatedList<GetMemberResponse>>
        GetMembersAsync(int pageIndex, int pageSize, bool noCache = true, CancellationToken cancel = default)
    {
        var result = await repository.GetMembersAsync(pageIndex, pageSize, noCache, cancel);
        return result;
    }

    // public async Task<CursorPaginatedList<GetMemberResponse>>
    //     GetMembersAsync(int pageSize, string nextPageToken, bool noCache = true, CancellationToken cancel = default)
    // {
    //     var result = await repository.GetMembersAsync(pageSize, nextPageToken, noCache, cancel);
    //     return result;
    // }
}