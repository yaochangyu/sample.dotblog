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
                                           IEnumerable<string> excludeProperties = null,
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
                ApplyAdd(dbContext, srcEmployee);
                break;
            case EntityState.Modified:
                ApplyModify(dbContext, srcEmployee, excludeProperties);

                break;
            case EntityState.Deleted:
                ApplyDelete(srcEmployee, dbContext);

                break;

            case EntityState.Unchanged:
                return 0;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return await dbContext.SaveChangesAsync(cancel);
    }

    private static void ApplyDelete(EmployeeEntity srcEmployee, EmployeeDbContext dbContext)
    {
        dbContext.Set<Employee>().Remove(new Employee() { Id = srcEmployee.Id });
    }

    private static void ApplyAdd(EmployeeDbContext dbContext, EmployeeEntity srcEmployee)
    {
        dbContext.Set<Employee>().Add(srcEmployee.To());
    }

    private static void ApplyModify(EmployeeDbContext dbContext,
                                    EmployeeEntity srcEmployee,
                                    IEnumerable<string> excludeProperties = null)
    {
        var destEmployee = new Employee()
        {
            Id = srcEmployee.Id
        };

        dbContext.Set<Employee>().Attach(destEmployee);
        var employeeEntry = dbContext.Entry(destEmployee);

        foreach (var property in srcEmployee.GetChangedProperties())
        {
            var propertyName = property.Key;
            var value = property.Value;
            if (excludeProperties != null
                && excludeProperties.Any(p => p == propertyName))
            {
                continue;
            }

            dbContext.Entry(destEmployee).Property(propertyName).CurrentValue = value;
            employeeEntry.Property(propertyName).IsModified = true;
        }
    }
}