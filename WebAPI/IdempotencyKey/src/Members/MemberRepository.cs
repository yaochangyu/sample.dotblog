using Microsoft.EntityFrameworkCore;

namespace IdempotencyKey.WebApi.Members;

public interface IMemberRepository
{
    Task<IReadOnlyList<Member>> GetAllAsync();
    Task<Member?> GetByIdAsync(Guid id);
    Task<Member> AddAsync(Member member);
    Task<Member?> UpdateAsync(Guid id, UpdateMemberRequest request);
    Task<bool> DeleteAsync(Guid id);
}

public class EfMemberRepository(MemberDbContext db) : IMemberRepository
{
    public async Task<IReadOnlyList<Member>> GetAllAsync() =>
        await db.Members.OrderBy(m => m.CreatedAt).ToListAsync();

    public async Task<Member?> GetByIdAsync(Guid id) =>
        await db.Members.FindAsync(id);

    public async Task<Member> AddAsync(Member member)
    {
        db.Members.Add(member);
        await db.SaveChangesAsync();
        return member;
    }

    public async Task<Member?> UpdateAsync(Guid id, UpdateMemberRequest request)
    {
        var member = await db.Members.FindAsync(id);
        if (member is null)
            return null;

        member.Name = request.Name;
        member.Email = request.Email;
        await db.SaveChangesAsync();
        return member;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var member = await db.Members.FindAsync(id);
        if (member is null)
            return false;

        db.Members.Remove(member);
        await db.SaveChangesAsync();
        return true;
    }
}
