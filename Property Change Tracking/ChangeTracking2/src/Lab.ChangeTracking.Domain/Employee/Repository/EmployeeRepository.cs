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
        this.ApplyModify<EmployeeEntity, Employee>(dbContext, srcEmployee, new List<string>
            {
                "Identity",
                "Addresses"
            }
        );
        this.ApplyModify<IdentityEntity, Identity>(dbContext, srcEmployee.Identity);
        this.ApplyChanges<AddressEntity, Address>(dbContext, srcEmployee.Addresses);
        return await dbContext.SaveChangesAsync(cancel);
    }
}