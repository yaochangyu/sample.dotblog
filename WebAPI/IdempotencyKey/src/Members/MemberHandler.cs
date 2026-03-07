using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;

namespace IdempotencyKey.WebApi.Members;

public class MemberHandler(IMemberRepository repository, MemberDbContext db)
{
    public async Task<Result<IReadOnlyList<Member>, Failure>> GetAllAsync(CancellationToken ct = default) =>
        await repository.GetAllAsync(ct);

    public async Task<Result<Member, Failure>> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await repository.GetByIdAsync(id, ct);

    public async Task<Result<Member, Failure>> CreateAsync(CreateMemberRequest request, CancellationToken ct = default)
    {
        // 驗證：Email 是否重複
        var duplicate = await db.Members.AsNoTracking().AnyAsync(m => m.Email == request.Email, ct);
        if (duplicate)
            return Result.Failure<Member, Failure>(new Failure
            {
                Code = nameof(FailureCode.DuplicateEmail),
                Message = $"Email '{request.Email}' 已被使用",
                IsRetryable = true
            });

        var member = new Member { Name = request.Name, Email = request.Email };
        return await repository.AddAsync(member, ct);
    }

    public async Task<Result<Member, Failure>> UpdateAsync(Guid id, UpdateMemberRequest request, CancellationToken ct = default)
    {
        // 驗證：Email 是否被其他會員使用
        var duplicate = await db.Members.AsNoTracking()
            .AnyAsync(m => m.Email == request.Email && m.Id != id, ct);
        if (duplicate)
            return Result.Failure<Member, Failure>(new Failure
            {
                Code = nameof(FailureCode.DuplicateEmail),
                Message = $"Email '{request.Email}' 已被使用",
                IsRetryable = true
            });

        return await repository.UpdateAsync(id, request, ct);
    }

    public async Task<Result<bool, Failure>> DeleteAsync(Guid id, CancellationToken ct = default) =>
        await repository.DeleteAsync(id, ct);
}
