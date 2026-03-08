using CSharpFunctionalExtensions;

namespace IdempotencyKey.WebApi.Members;

public class MemberHandler(IMemberRepository repository)
{
    public async Task<Result<IReadOnlyList<Member>, Failure>> GetAllAsync(CancellationToken ct = default) =>
        await repository.GetAllAsync(ct);

    public async Task<Result<Member, Failure>> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await repository.GetByIdAsync(id, ct);

    public async Task<Result<Member, Failure>> CreateAsync(CreateMemberRequest request, CancellationToken ct = default)
    {
        var member = new Member { Name = request.Name, Email = request.Email };
        return await repository.AddAsync(member, ct);
    }

    public async Task<Result<Member, Failure>> UpdateAsync(Guid id, UpdateMemberRequest request, CancellationToken ct = default) =>
        await repository.UpdateAsync(id, request, ct);

    public async Task<Result<bool, Failure>> DeleteAsync(Guid id, CancellationToken ct = default) =>
        await repository.DeleteAsync(id, ct);
}
