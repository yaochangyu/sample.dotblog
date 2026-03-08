using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;

namespace IdempotencyKey.WebApi.Members;

public interface IMemberRepository
{
    Task<Result<IReadOnlyList<Member>, Failure>> GetAllAsync(CancellationToken ct = default);
    Task<Result<Member, Failure>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<Member, Failure>> AddAsync(Member member, CancellationToken ct = default);
    Task<Result<Member, Failure>> UpdateAsync(Guid id, UpdateMemberRequest request, CancellationToken ct = default);
    Task<Result<bool, Failure>> DeleteAsync(Guid id, CancellationToken ct = default);
}

public class EfMemberRepository(MemberDbContext db) : IMemberRepository
{
    public async Task<Result<IReadOnlyList<Member>, Failure>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var members = await db.Members.AsNoTracking().OrderBy(m => m.CreatedAt).ToListAsync(ct);
            return Result.Success<IReadOnlyList<Member>, Failure>(members);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<Member>, Failure>(new Failure
            {
                Code = nameof(FailureCode.DbError),
                Message = "查詢會員清單時發生錯誤",
                Exception = ex
            });
        }
    }

    public async Task<Result<Member, Failure>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var member = await db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
            if (member is null)
                return Result.Failure<Member, Failure>(new Failure
                {
                    Code = nameof(FailureCode.NotFound),
                    Message = $"找不到 Id 為 {id} 的會員"
                });
            return Result.Success<Member, Failure>(member);
        }
        catch (Exception ex)
        {
            return Result.Failure<Member, Failure>(new Failure
            {
                Code = nameof(FailureCode.DbError),
                Message = "查詢會員時發生錯誤",
                Exception = ex
            });
        }
    }

    public async Task<Result<Member, Failure>> AddAsync(Member member, CancellationToken ct = default)
    {
        try
        {
            db.Members.Add(member);
            await db.SaveChangesAsync(ct);
            return Result.Success<Member, Failure>(member);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Result.Failure<Member, Failure>(new Failure
            {
                Code = nameof(FailureCode.DuplicateEmail),
                Message = $"Email '{member.Email}' 已被使用",
                IsRetryable = true
            });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Result.Failure<Member, Failure>(new Failure
            {
                Code = nameof(FailureCode.DbConcurrency),
                Message = "資料衝突，請稍後再試",
                IsRetryable = true,
                Exception = ex
            });
        }
        catch (Exception ex)
        {
            return Result.Failure<Member, Failure>(new Failure
            {
                Code = nameof(FailureCode.DbError),
                Message = "新增會員時發生錯誤",
                Exception = ex
            });
        }
    }

    public async Task<Result<Member, Failure>> UpdateAsync(Guid id, UpdateMemberRequest request, CancellationToken ct = default)
    {
        try
        {
            var member = await db.Members.FindAsync([id], ct);
            if (member is null)
                return Result.Failure<Member, Failure>(new Failure
                {
                    Code = nameof(FailureCode.NotFound),
                    Message = $"找不到 Id 為 {id} 的會員"
                });

            member.Name = request.Name;
            member.Email = request.Email;
            await db.SaveChangesAsync(ct);
            return Result.Success<Member, Failure>(member);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Result.Failure<Member, Failure>(new Failure
            {
                Code = nameof(FailureCode.DuplicateEmail),
                Message = $"Email '{request.Email}' 已被使用",
                IsRetryable = true
            });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Result.Failure<Member, Failure>(new Failure
            {
                Code = nameof(FailureCode.DbConcurrency),
                Message = "資料衝突，請稍後再試",
                IsRetryable = true,
                Exception = ex
            });
        }
        catch (Exception ex)
        {
            return Result.Failure<Member, Failure>(new Failure
            {
                Code = nameof(FailureCode.DbError),
                Message = "更新會員時發生錯誤",
                Exception = ex
            });
        }
    }

    public async Task<Result<bool, Failure>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var member = await db.Members.FindAsync([id], ct);
            if (member is null)
                return Result.Failure<bool, Failure>(new Failure
                {
                    Code = nameof(FailureCode.NotFound),
                    Message = $"找不到 Id 為 {id} 的會員"
                });

            db.Members.Remove(member);
            await db.SaveChangesAsync(ct);
            return Result.Success<bool, Failure>(true);
        }
        catch (Exception ex)
        {
            return Result.Failure<bool, Failure>(new Failure
            {
                Code = nameof(FailureCode.DbError),
                Message = "刪除會員時發生錯誤",
                Exception = ex
            });
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException is Npgsql.PostgresException { SqlState: "23505" };
}

