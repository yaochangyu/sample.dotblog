using System.Linq;
using ChangeTracking;
using EFCore.BulkExtensions;
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
        var employee =
            await dbContext.Employees.FirstOrDefaultAsync(a => a.Id == srcEmployee.Id, cancellationToken: cancel);
        var destEmployee = this.To(srcEmployee);
        var updateColumns = tracked.ChangedProperties.ToList();
        var changeCount = await dbContext.Employees
                                         .Where(a => a.Id == srcEmployee.Id)
                                         .BatchUpdateAsync(destEmployee, updateColumns, cancel);

        return changeCount;
    }

    public Employee To(EmployeeEntity srcEmployee)
    {
        return new Employee
        {
            Id = srcEmployee.Id,
            Name = srcEmployee.Name,
            Age = srcEmployee.Age,
            Remark = srcEmployee.Remark,
            CreateAt = srcEmployee.CreateAt,
            CreateBy = srcEmployee.CreateBy,
        };
    }
}