using CSharpFunctionalExtensions;
using Lab.Sharding.Infrastructure;
using Lab.Sharding.Infrastructure.TraceContext;

namespace Lab.Sharding.WebAPI.Member;

public class MemberHandler(
	MemberRepository repository,
	IContextGetter<TraceContext?> traceContextGetter,
	ILogger<MemberHandler> logger)
{
	public async Task<PaginatedList<GetMemberResponse>>
		GetMembersAsync(int pageIndex, int pageSize, bool noCache = true, CancellationToken cancel = default)
	{
		var result = await repository.GetMembersAsync(pageIndex, pageSize, noCache, cancel);
		return result;
	}

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
				Data = src
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
				Data = src
			});
		}

		return Result.Success<Member, Failure>(src);
	}
}