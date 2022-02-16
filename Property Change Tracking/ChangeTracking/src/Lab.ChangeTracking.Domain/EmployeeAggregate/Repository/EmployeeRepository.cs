using ChangeTracking;
using Lab.ChangeTracking.Domain.EmployeeAggregate.Entity;
using Lab.ChangeTracking.Infrastructure.DB.EntityModel;
using Microsoft.EntityFrameworkCore;

namespace Lab.ChangeTracking.Domain.EmployeeAggregate.Repository;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly IDbContextFactory<EmployeeDbContext> _memberContextFactory;

    public EmployeeRepository(IDbContextFactory<EmployeeDbContext> memberContextFactory)
    {
        this._memberContextFactory = memberContextFactory;
    }

    public async Task<int> ChangeAsync(EmployeeEntity srcEmployee,
                                       CancellationToken cancel = default)
    {
        var tracked = srcEmployee.CastToIChangeTrackable();

        await using var dbContext = await this._memberContextFactory.CreateDbContextAsync(cancel);
        foreach (var changedProperty in tracked.ChangedProperties)
        {
            
        }

        return await dbContext.SaveChangesAsync(cancel);
    }
}