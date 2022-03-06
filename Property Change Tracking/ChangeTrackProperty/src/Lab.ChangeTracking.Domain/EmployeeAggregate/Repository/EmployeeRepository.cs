using Lab.ChangeTracking.Infrastructure.DB.EntityModel;
using Microsoft.EntityFrameworkCore;

namespace Lab.ChangeTracking.Domain;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly IDbContextFactory<EmployeeDbContext> _employeeDbContextFactory;

    public EmployeeRepository(IDbContextFactory<EmployeeDbContext> memberContextFactory)
    {
        this._employeeDbContextFactory = memberContextFactory;
    }

    public async Task<int> SaveChangeAsync(EmployeeEntity srcEmployee,
                                           CancellationToken cancel = default)
    {
        if (srcEmployee.CommitState != CommitState.Accepted)
        {
            throw new Exception($"{nameof(srcEmployee)} 尚未核准，不得儲存");
        }

        await using var dbContext = await this._employeeDbContextFactory.CreateDbContextAsync(cancel);
        switch (srcEmployee.EntityState)
        {
            case EntityState.Added:
                dbContext.Set<Employee>().Add(srcEmployee.To());
                break;
            case EntityState.Modified:
                break;
            case EntityState.Deleted:
                break;
           
            default:
                throw new ArgumentOutOfRangeException();
        }

        return await dbContext.SaveChangesAsync(cancel);
    }
}