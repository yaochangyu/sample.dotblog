using Lab.ChangeTracking.Infrastructure.DB.EntityModel;
using Microsoft.EntityFrameworkCore;

namespace Lab.ChangeTracking.Domain;

public class EmployeeRepository : RepositoryBase, IEmployeeRepository
{
    private readonly IDbContextFactory<EmployeeDbContext> _memberContextFactory;

    public EmployeeRepository(IDbContextFactory<EmployeeDbContext> memberContextFactory)
    {
        this._memberContextFactory = memberContextFactory;
    }

    public async Task<int> SaveChangeAsync(EmployeeEntity srcEmployee,
                                           CancellationToken cancel = default)
    {
        await using var dbContext = await this._memberContextFactory.CreateDbContextAsync(cancel);
        var destEmployee = srcEmployee.To();
        this.ApplyChange(dbContext, srcEmployee, destEmployee, new List<string>
            {
                "Identity",
                "Addresses"
            }
        );
        this.ApplyChange(dbContext, srcEmployee.Identity, destEmployee.Identity);
        this.ApplyChanges(dbContext, srcEmployee.Addresses, destEmployee.Addresses);
        return await dbContext.SaveChangesAsync(cancel);
    }
}